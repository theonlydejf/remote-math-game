
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Threading.Tasks;

class Program
{
    static object ConsoleOutputLock = new object();

    static async Task Main(string[] args)
    {

        var cts = new CancellationTokenSource();
        var listener = new TcpListener(IPAddress.Any, 1223);
        listener.Start();
        Console.WriteLine("Server started...");

        while (!cts.Token.IsCancellationRequested)
        {
            TcpClient acceptedClient = await listener.AcceptTcpClientAsync();

            var clientEndpoint = acceptedClient.Client.RemoteEndPoint as IPEndPoint;
            string? clientIp = clientEndpoint?.Address.ToString();

            lock (ConsoleOutputLock)
            {
                if (clientIp == null)
                {
                    Console.WriteLine("Client without IP connected :o, discarding...");
                    continue;
                }

                _ = HandleClientAsync(acceptedClient, cts.Token);

                Console.WriteLine($"Client connected: {clientIp}");
            }
        }

        listener.Stop();
    }

    private static void DiscardClient(TcpClient client)
    {
        StreamWriter sw = new StreamWriter(client.GetStream());
        sw.WriteLine("Client with the same IP is already connected!");
        client.Close();
    }

    private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (client)
        {
            var buffer = new byte[1024];
            var stream = client.GetStream();

            while (!token.IsCancellationRequested)
            {
                int bytesRead;
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (bytesRead == 0)
                {
                    Console.WriteLine("Client disconnected...");
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received: {message}");

                var response = Encoding.UTF8.GetBytes("Message received");
                await stream.WriteAsync(response, 0, response.Length, token);
            }
        }
    }
}
