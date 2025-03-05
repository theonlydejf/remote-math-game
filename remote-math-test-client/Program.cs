using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public enum ParserDifficulty { EASY, MEDIUM, HARD, ADVANCED }

/// <summary>
/// Defines difficulty of the current session of the Math Test
/// </summary>
public struct Difficulty
{

    public bool AllowSigns { get; }
    public bool AllowMessages { get; }
    public ParserDifficulty ParserDifficulty { get; }

    public Difficulty(bool allowSigns, bool allowMessages, ParserDifficulty parserDifficulty)
    {
        AllowSigns = allowSigns;
        AllowMessages = allowMessages;
        ParserDifficulty = parserDifficulty;
    }

    public override readonly string ToString()
    {
        return $"{AllowSigns};{AllowMessages};{ParserDifficulty}";
    }
}

public static class MathTest
{
    private static TcpClient? client;
    private static string? lastLine;
    public static StreamReader? Reader { get; private set; }
    public static string Status { get; private set; } = "not finished yet";
    public static double Score { get; private set; } = -1;
    public static string? StatusParamRaw { get; private set; } = null;
    public static StreamWriter? Writer { get; private set; }
    public static bool IsTestRunning { get; private set; } = false;

    public static void Connect(string serverIP, int port, Difficulty difficulty, string username)
    {
        if(client != null)
            throw new InvalidOperationException("You can only connect once per session");

        // Connect to the server
        client = new TcpClient();
        client.Connect(serverIP, port);

        Writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
        Reader = new StreamReader(client.GetStream());

        // Send settings
        Writer.WriteLine(difficulty.ToString());
        Thread.Sleep(100);
        Writer.WriteLine(username);

        IsTestRunning = true;
    }

    public static string? ReadLine()
    {
        if(Reader == null)
            throw new InvalidOperationException("Test not connected");
        
        if(!IsTestRunning)
            return null;

        string? curr = Reader.ReadLine();
        if(curr != null)
            lastLine = curr;
        else
            return null;

        // Check if command sequence was received
        if(curr.Contains("&|"))
        {
            IsTestRunning = false;
            ProcessStatus();
            return null;
        }
        return curr;
    }

    public static void WriteLine(string value)
    {
        if(Writer == null)
            throw new InvalidOperationException("Test not connected");
        Writer.WriteLine(value);
        Writer.Flush();
    }

    public static void PlayOnConsole()
    {
        if(Reader == null || Writer == null)
            throw new InvalidOperationException("Not connected to math test");

        bool connected = true;

        // Receiver Thread - Listens for messeges from the server and redirects them
        //                   to the console standart output
        _ = Task.Run(async () =>
        {
            while (true)
            {
                string? response = await Reader.ReadLineAsync();
                if (response == null)
                {
                    connected = false;
                    break;
                }

                if(!response.Contains("&|")) // If not status command
                    Console.WriteLine(response);
                else
                    lastLine = response;
            }

            // Print results
            IsTestRunning = false;
            Console.ForegroundColor = ConsoleColor.Yellow;
            if(lastLine == null)
                Console.WriteLine("No status received.");
            else
            {
                ProcessStatus();
                switch (Status)
                {
                    case "err":
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Fatal error: {StatusParamRaw}");
                        break;
                    case "incorrect":
                        Console.WriteLine($"Your answer was incorrect. Score: {Score:0.00}");
                        break;
                    case "timeout":
                        Console.WriteLine($"Time is out. Score: {Score:0.00}");
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Fatal error: Unknown status: '{Status}', received param: {StatusParamRaw}");
                        break;
                }
                Console.ForegroundColor = ConsoleColor.Yellow;
            }

            Console.WriteLine("Press enter to exit...");
        });

        // Listen for the user input and send it to the server
        while (connected)
        {
            string? input = Console.ReadLine();
            if (input == null)
                break;
            Writer.WriteLine(input);
        }
    }

    private static void ProcessStatus()
    {
        // No status command was received
        if(lastLine == null)
        {
            Status = "invalid";
            return;
        }
        
        // Parse the results
        string[] status = lastLine.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if(status.Length < 2)
        {
            Status = "err";
            StatusParamRaw = "Invalid status command received";
            return;
        }

        StatusParamRaw = status[status.Length - 1];
        if(!double.TryParse(StatusParamRaw, out double score))
        {
            Status = "invalid score";
            Score = -1;
            return;
        }

        Score = score;
        Status = status[status.Length - 2];
    }
}

class Program
{
    static void Main(string[] args)
    {
        string username = "unnamed"; // Your username
        string ipAddress = "localhost"; // IP Address of the server
        int port = 12345; // Port on which the server is running
        Difficulty difficulty = new Difficulty // Difficulty of the test
        (
            false, // Allow signs?
            false, // Allow messages?
            ParserDifficulty.EASY // Equiation format difficulty level
        );
        
        MathTest.Connect(ipAddress, port, difficulty, username);

        /* Your code here */
    }
}