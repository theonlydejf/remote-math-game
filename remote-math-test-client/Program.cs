﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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
    private static string? lastLine;
    public static StreamReader? Reader { get; private set; }
    public static string Status { get; private set; } = "not finished yet";
    public static double Score { get; private set; } = -1;
    public static StreamWriter? Writer { get; private set; }
    public static bool IsTestRunning { get; private set; } = false;

    public static void Connect(string serverIP, int port, Difficulty difficulty, string username)
    {
        if(client != null)
            throw new InvalidOperationException("You can only connect once per session");
        client = new TcpClient();
        client.Connect(serverIP, port);
        Writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
        Reader = new StreamReader(client.GetStream());
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

        if(curr.Contains('|'))
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
                if(!response.Contains('|'))
                    Console.WriteLine(response);
                else
                    lastLine = response;
            }
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
                        Console.WriteLine($"Fatal error: {Score:0.00}");
                        break;
                    case "incorrect":
                        Console.WriteLine($"Your answer was incorrect. Score: {Score:0.00}");
                        break;
                    case "timeout":
                        Console.WriteLine($"Time is out. Score: {Score:0.00}");
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
            Writer.WriteLine(input);
        }
    }

    private static void ProcessStatus()
    {
        if(lastLine == null)
        {
            Status = "invalid";
            return;
        }
        string[] status = lastLine.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if(!double.TryParse(status[status.Length - 1], out double score))
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
        MathTest.Connect("localhost", 12345, new Difficulty(true, true, ParserDifficulty.ADVANCED), "david");
        string pattern = "([+-]?\\d+([.,]\\d+)?)\\s*([\\+\\-\\*\\/])\\s*([+-]?\\d+([.,]\\d+)?)";

        Console.WriteLine("Working...");
        while(MathTest.IsTestRunning)
        {
            string? str = MathTest.ReadLine();
            if(str == null)
                continue;
            
            Match match = Regex.Match(str, pattern);

            if (match.Success)
            {
                double firstNumber = Convert.ToDouble(match.Groups[1].Value);
                string operation = match.Groups[3].Value;
                double secondNumber = Convert.ToDouble(match.Groups[4].Value);

                switch (operation)
                {
                    case "+":
                        MathTest.WriteLine((firstNumber + secondNumber).ToString());
                        break;
                    case "-":
                        MathTest.WriteLine((firstNumber - secondNumber).ToString());
                        break;
                    case "*":
                        MathTest.WriteLine((firstNumber * secondNumber).ToString());
                        break;
                    case "/":
                        MathTest.WriteLine((firstNumber / secondNumber).ToString());
                        break;
                }
            }
        }
        Console.WriteLine(MathTest.Status);
    }
}