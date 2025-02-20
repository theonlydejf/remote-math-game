
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

class Program
{
    private const int Port = 12345;
    private const int GameDurationSeconds = 10;

    static async Task Main(string[] args)
    {
        TcpListener listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        Console.WriteLine($"Server started on port {Port}.");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        Console.Write($"Client connected on {((IPEndPoint)client.Client.RemoteEndPoint).Address}");
        using (client)
        {
            NetworkStream stream = client.GetStream();
            using StreamReader sr = new(stream);
            using StreamWriter sw = new(stream);

            string? difficultyStr = await sr.ReadLineAsync();

            if(!Difficulty.TryParse(difficultyStr, out Difficulty? difficulty) || difficulty == null)
            {
                sw.WriteLine("|err|Bad difficulty format");
                sw.Flush();
                client.Close();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" - Bad difficulty");
                Console.ResetColor();
                return;
            }

            string? username = await sr.ReadLineAsync();
            if(username == null)
            {
                sw.WriteLine("|err|Invalid or no username");
                sw.Flush();
                client.Close();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" - Bad username");
                Console.ResetColor();
                return;
            } 

            Console.WriteLine($" ({username})");

            GameManager gameManager = new(difficulty.Value);
            int completed = 0;
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(GameDurationSeconds));

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    if (!await gameManager.PlayOnce(sw, sr, cts.Token))
                    {
                        sw.Write("|incorrect|");
                        break;
                    }
                    completed++;
                }
            }
            catch (OperationCanceledException) {}
            if(cts.IsCancellationRequested)
                sw.Write("|timeout|");
            
            double score = Math.Pow(completed, difficulty.Value.ScoreExponent) * difficulty.Value.ScoreMultiplier;
            sw.WriteLine(score.ToString("0.00"));
            Console.WriteLine($"Score for {username}: {score:0.00}");
            sw.Flush();
            client.Close();
        }
    }
}