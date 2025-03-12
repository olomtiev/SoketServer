using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SoketServer
{
    class Program
    {
        private static ConcurrentDictionary<TcpClient, (string Ip, int Sum)> clients = new();
        private static int port;

        static async Task Main(string[] args)
        {
            if (args.Length != 1 || !int.TryParse(args[0], out port))
            {
                Console.WriteLine("Usage: server.exe <port>");
                return;
            } 
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Console.WriteLine($"Server started on port {port}");

            while (true)
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            clients[client] = (clientIp, 0);
            try
            {
                using NetworkStream stream = client.GetStream();
                using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                using StreamReader reader = new StreamReader(stream, Encoding.UTF8);

                await writer.WriteLineAsync("Enter numbers to sum, or 'list'");

                while (client.Connected)
                {
                    string input = await reader.ReadLineAsync();
                    if (input == null) break;

                    if (input.Trim().ToLower() == "list")
                    {
                        var clientList = string.Join("\n", clients.Select(c => $"{c.Value.Ip}: {c.Value.Sum}"));
                        await writer.WriteLineAsync(clientList);
                    }
                    else if (int.TryParse(input, out int number))
                    {
                        clients[client] = (clientIp, clients[client].Sum + number);
                        await writer.WriteLineAsync($"Current sum: {clients[client].Sum}");
                    }
                    else
                    {
                        await writer.WriteLineAsync("Invalid input. Enter a number or 'list'.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client {clientIp} error: {ex.Message}");
            }
            finally
            {
                client.Close();
                clients.TryRemove(client, out _);
                Console.WriteLine($"Client disconnected: {clientIp}");
            }
        }
    }
}