using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public enum ParserDifficulty { EASY, MEDIUM, HARD, ADVANCED }

public struct Difficulty
{
    private static Dictionary<ParserDifficulty, double> parser2expMap = new()
    {
        { ParserDifficulty.EASY, 1 },
        { ParserDifficulty.MEDIUM, 1.5 },
        { ParserDifficulty.HARD, 2 },
        { ParserDifficulty.ADVANCED, 3 }
    };

    private const double SIGNS_SCORE_MUL = 2;
    private const double MSG_SCORE_MUL = 3;

    public double ScoreExponent { get; }
    public double ScoreMultiplier { get; }

    public bool AllowSigns { get; }
    public bool AllowMessages { get; }
    public ParserDifficulty ParserDifficulty { get; }

    public Difficulty(bool allowSigns, bool allowMessages, ParserDifficulty parserDifficulty)
    {
        AllowSigns = allowSigns;
        AllowMessages = allowMessages;
        ParserDifficulty = parserDifficulty;

        ScoreExponent = parser2expMap[parserDifficulty];
        ScoreMultiplier = 1;
        if(AllowSigns)
            ScoreExponent *= SIGNS_SCORE_MUL;
        if(AllowMessages)
            ScoreExponent *= MSG_SCORE_MUL;
    }

    public static bool TryParse(string str, out Difficulty? difficulty)
    {
        difficulty = null;
        string[] modeIDs = str.ToLower().Split(';');
        if(modeIDs.Length != 3)
            return false;

        bool allowSigns = modeIDs[0].StartsWith('t');
        bool allowMessages = modeIDs[1].StartsWith('t');
        bool validParserDifficulty = Enum.TryParse(modeIDs[2].ToUpper(), out ParserDifficulty parserDifficulty);
        if(!validParserDifficulty)
            return false;
        
        difficulty = new(allowSigns, allowMessages, parserDifficulty);
        return true;
    }

    public override string ToString()
    {
        return $"{AllowSigns};{AllowMessages};{ParserDifficulty}";
    }
}

public static class MathTest
{
    private static TcpClient? client;
    public static StreamReader? reader { get; private set; }
    public static StreamWriter? writer { get; private set; }

    public static void Connect(string serverIP, int port, Difficulty difficulty)
    {
        client = new TcpClient();
        client.Connect(serverIP, port);
        writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
        reader = new StreamReader(client.GetStream());
        writer.WriteLine(difficulty.ToString());
    }

    public static string? ReadLine()
    {
        return reader?.ReadLine();
    }

    public static void WriteLine(string value)
    {
        writer?.WriteLine(value);
        writer?.Flush();
    }

    public static void PlayOnConsole()
    {
        if(reader == null || writer == null)
            throw new InvalidOperationException("Not connected to math test");

        bool connected = true;

        _ = Task.Run(async () =>
        {
            string? statusLine = null;
            while (true)
            {
                string? response = await reader.ReadLineAsync();
                if (response == null)
                {
                    connected = false;
                    break;
                }
                if(!response.Contains('|'))
                    Console.WriteLine(response);
                else
                    statusLine = response;
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            if(statusLine == null)
                Console.WriteLine("No status received.");
            else
            {
                string[] status = statusLine.Split('|', StringSplitOptions.RemoveEmptyEntries);
                switch (status[^2])
                {
                    case "err":
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Fatal error: " + status[^1]);
                        break;
                    case "incorrect":
                        Console.WriteLine("Your answer was incorrect. Score: " + status[^1]);
                        break;
                    case "timeout":
                        Console.WriteLine("Time is out. Score: " + status[^1]);
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Fatal error: Unknown status");
                        break;
                }
                Console.ForegroundColor = ConsoleColor.Yellow;
            }

            Console.WriteLine("Press enter to exit...");
        });

        while (connected)
        {
            string? input = Console.ReadLine();
            if (input == null)
                break;
            writer.WriteLine(input);
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        MathTest.Connect("localhost", 12345, new Difficulty(false, false, ParserDifficulty.EASY));
        MathTest.PlayOnConsole();
    }
}