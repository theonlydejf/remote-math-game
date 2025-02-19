using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public static class MathTest
{
    private static TcpClient? client;
    public static StreamReader? reader { get; private set; }
    public static StreamWriter? writer { get; private set; }

    public static void Connect(string serverIP, int port)
    {
        client = new TcpClient();
        client.Connect(serverIP, port);
        writer = new StreamWriter(client.GetStream());
        reader = new StreamReader(client.GetStream());
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
}

class Program
{
    static void Main(string[] args)
    {
        MathTest.Connect("localhost", 1223);
        while(true)
        {
            MathTest.WriteLine("ahoj");

            if(Console.ReadLine() == "konec")
                break;
        }
    }
}