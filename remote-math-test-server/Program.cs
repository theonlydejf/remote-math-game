
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Security.AccessControl;

struct Highscore
{
    public string Username { get; set; }
    public double Score { get; set; }

    public Highscore(string username, double score)
    {
        Username = username;
        Score = score;
    }

    public readonly override int GetHashCode() => Username == null ? 0 : Username.GetHashCode();

    public readonly override bool Equals(object? obj) => obj is Highscore other && Equals(other);

    public readonly bool Equals(Highscore other) => string.Equals(Username, other.Username, StringComparison.Ordinal);
}

class Program
{
    private const string HIGHSCORE_FILE_PATH = "highscore-global.csv";
    private const string PERSONAL_HIGHSCORE_FILE_PATH = "highscores-personal.csv";
    private static readonly Regex CSVParseSeparatorRegex = new(@"(?<!\\),");
    private static readonly Regex CSVParseEscapedRegex = new(@"\\(.)");
    private static readonly Regex CSVEncodeEscapedRegex = new(@"([\\,])");
    private static object ScoreLock = new();
    private static object ConsoleLock = new();
    private static Highscore SessionHighscore = new("no one", 0);
    private static Highscore GlobalHighscore = new("no one", 0);
    private static Dictionary<string, double> PersonalHighscores = new();

    private const int Port = 12345;
    private const int GameDurationSeconds = 10;

    private const int SCOREBOARD_WIDTH = 30;

    private static IEnumerable<string[]> ParseCSV(Stream stream)
    {
        List<string[]> output = new();
        using(StreamReader sr = new(stream))
        {
            string? currLine;
            while((currLine = sr.ReadLine()) != null)
            {
                string[] row = CSVParseSeparatorRegex.Split(currLine) // Get each element
                                .Select(x => CSVParseEscapedRegex.Replace(x, "$1")) // Handle escape sequences
                                .ToArray();
                output.Add(row);               
            }
        }
        return output;
    }

    private static void EncodeCSV(Stream stream, IEnumerable<string[]> rows)
    {
        using(StreamWriter sw = new(stream) { AutoFlush = true })
        {
            foreach(string[] row in rows)
            {
                var escaped = row.Select(x => CSVEncodeEscapedRegex.Replace(x, @"\$1"));
                sw.WriteLine(string.Join(',', escaped));
            }
        }
    }

    private static void SaveGlobalHighScore()
    {
        using FileStream fs = new(HIGHSCORE_FILE_PATH, FileMode.OpenOrCreate, FileAccess.Write);

        EncodeCSV(fs, new List<string[]> { new string[] { GlobalHighscore.Username, GlobalHighscore.Score.ToString() } });
    }

    private static void SavePersonalHighScores()
    {
        using FileStream fs = new(PERSONAL_HIGHSCORE_FILE_PATH, FileMode.OpenOrCreate, FileAccess.Write);

        List<string[]> scores = new List<string[]>();
        foreach(var score in PersonalHighscores)
            scores.Add(new string[] { score.Key, score.Value.ToString() });
    
        EncodeCSV(fs, scores);
    }

    private static void PrintBoxedText(string[] lines, ConsoleColor bgColor, int baseWidth, bool right = false, string? header = null)
    {
        foreach(string s in lines)
            if(s.Length > baseWidth)
                baseWidth = s.Length;
        if(header != null && header.Length + 2 > baseWidth)
            baseWidth = header.Length + 2;
        
        if(right)
            Console.CursorLeft = Console.WindowWidth - baseWidth - 4;
        Console.BackgroundColor = bgColor;
        if(header == null)
            Console.Write($"+{new string('-', baseWidth+2)}+");
        else
            Console.Write($"+- {header} {new string('-', baseWidth-header.Length-2)}-+");
        Console.ResetColor();
        if(!right)
            Console.WriteLine(new string(' ', Console.WindowWidth-(baseWidth+4)));
        foreach(string msg in lines)
        {
            if(right)
                Console.CursorLeft = Console.WindowWidth - baseWidth - 4;
            Console.BackgroundColor = bgColor;
            Console.Write($"| {msg.PadRight(baseWidth)} |");
            Console.ResetColor();
            if(!right)
                Console.WriteLine(new string(' ', Console.WindowWidth-(baseWidth+4)));
        }

        if(right)
            Console.CursorLeft = Console.WindowWidth - baseWidth - 4;
        Console.BackgroundColor = bgColor;
        Console.Write($"+{new string('-', baseWidth+2)}+");
        Console.ResetColor();
        if(!right)
            Console.WriteLine(new string(' ', Console.WindowWidth-(baseWidth+4)));
    }

    private static void PrintScoreboard(bool rememberCursor = true)
    {
        (int x, int y) = (Console.CursorLeft, Console.CursorTop);

        string[] globalHighscoresText = new string[]
        {
            $"Session best: {SessionHighscore.Score} ({SessionHighscore.Username})",
            $"Global best: {GlobalHighscore.Score} ({GlobalHighscore.Username})"
        };

        string[] personalHighscoresText = PersonalHighscores.Select(x => $"{x.Key}: {x.Value}").ToArray();

        Console.SetCursorPosition(0, 0);
        PrintBoxedText(globalHighscoresText, ConsoleColor.Blue, SCOREBOARD_WIDTH);
        int maxY = Console.CursorTop;
        if(personalHighscoresText.Length > 0)
        {
            Console.SetCursorPosition(0, 0);
            PrintBoxedText(personalHighscoresText, ConsoleColor.DarkGray, SCOREBOARD_WIDTH, true, "Personal highscores");
        }
        if(Console.CursorTop > maxY)
            maxY = Console.CursorTop;
        
        if(rememberCursor && Console.CursorTop < y)
            Console.SetCursorPosition(x, y);
        else
            Console.SetCursorPosition(0, maxY);
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        string msg = "";
        if (client.Client.RemoteEndPoint is IPEndPoint remoteEndPoint)
            msg = $"Client connected on {remoteEndPoint.Address}";
        else
            msg = "Client connected on unknown address";

        using (client)
        {
            NetworkStream stream = client.GetStream();
            using StreamReader sr = new(stream);
            using StreamWriter sw = new(stream);

            string? difficultyStr = await sr.ReadLineAsync();

            if(!Difficulty.TryParse(difficultyStr, out Difficulty? difficulty) || difficulty == null)
            {
                sw.WriteLine("&|err|Bad difficulty format");
                sw.Flush();
                client.Close();
                lock(ConsoleLock)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(msg + " - Bad difficulty");
                    Console.ResetColor();

                    PrintScoreboard();
                }
                return;
            }

            string? username = await sr.ReadLineAsync();

            lock(ConsoleLock)
            {
                if (username == null || username.Length > 20 || username.Length < 3)
                {
                    sw.WriteLine("&&|err|Invalid or no username");
                    sw.Flush();
                    client.Close();

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(msg + " - Bad username");
                    Console.ResetColor();
                    PrintScoreboard();
                    return;
                }

                Console.WriteLine(msg + $" ({username})");
                PrintScoreboard();
            }

            GameManager gameManager = new(difficulty.Value);
            int completed = 0;
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(GameDurationSeconds));

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    if (!await gameManager.PlayOnce(sw, sr, cts.Token))
                    {
                        sw.Write("$&|incorrect|");
                        break;
                    }
                    completed++;
                }
            }
            catch (OperationCanceledException) {}
            if(cts.IsCancellationRequested)
                sw.Write("$&|timeout|");
            
            Highscore score = new
            (
                username, 
                Math.Round(Math.Pow(completed, difficulty.Value.ScoreExponent) * difficulty.Value.ScoreMultiplier * .01, 2)
            );
            lock(ScoreLock)
            {
                if(!PersonalHighscores.TryGetValue(username, out double previousHighscore) || score.Score > previousHighscore)
                {
                    PersonalHighscores[username] = score.Score;
                    SavePersonalHighScores();
                    
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{username} has new personal best!");
                    Console.ResetColor();
                }

                if(score.Score > SessionHighscore.Score)
                {
                    SessionHighscore = score;
                    
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{username} has set new record for the current session!");
                    Console.ResetColor();
                }

                if (score.Score > GlobalHighscore.Score)
                {
                    GlobalHighscore = score;
                    SaveGlobalHighScore();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{username} has set new global record!");
                    Console.ResetColor();
                }
            }
            sw.WriteLine(score.Score.ToString("0.00"));
            lock(ConsoleLock)
            {
                Console.WriteLine($"Score for {username}: {score.Score:0.00}");
                PrintScoreboard();
            }
            sw.Flush();
            client.Close();
        }
    }

    static async Task Main(string[] args)
    {
        try
        {
            using (FileStream fs = new(HIGHSCORE_FILE_PATH, FileMode.OpenOrCreate, FileAccess.Read))
            {
                var globalHighScore = ParseCSV(fs).Select(x => new Highscore(x[0], Convert.ToDouble(x[1])));
                if(globalHighScore.Any())
                    GlobalHighscore = globalHighScore.First();
            }
            using (FileStream fs = new(PERSONAL_HIGHSCORE_FILE_PATH, FileMode.OpenOrCreate, FileAccess.Read))
            {
                var personalHighscores = ParseCSV(fs).Select(x => new Highscore(x[0], Convert.ToDouble(x[1])));
                foreach(Highscore score in personalHighscores)
                    PersonalHighscores.Add(score.Username, score.Score);
            }
        }
        catch (Exception ex) when (ex is FormatException || ex is IndexOutOfRangeException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Highscore file has bad format, deleting...");
            File.Delete(HIGHSCORE_FILE_PATH);
            File.Delete(PERSONAL_HIGHSCORE_FILE_PATH);
            Console.WriteLine("Try again");
            return;
        }

        Console.Clear();
        TcpListener listener = new(IPAddress.Any, Port);
        listener.Start();
        PrintScoreboard(false);
        Console.WriteLine($"Server started on port {Port}.");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }
}