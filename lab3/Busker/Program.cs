using System;
using Microsoft.AspNetCore.SignalR.Client;

namespace Busker
{
    class Program
    {
        static void Main(string[] args)
        {
            var conn = new HubConnectionBuilder()
                .WithUrl("http://localhost:3000/hubs/orchestrator")
                .WithConsoleLogger()
                .Build();

            conn.StartAsync().Wait();
            conn.InvokeAsync("test").Wait();

            conn.StopAsync().Wait();

            Console.WriteLine("Message sent.");
        }
    }
}
