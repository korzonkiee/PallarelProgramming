using System;
using Microsoft.AspNetCore.SignalR.Client;

namespace Busker
{
    class Program
    {
        static void Main(string[] args)
        {
            var buskersLoader = new BuskersLoader();
            var musicians = buskersLoader
                .LoadBuskersFromFile(Files.BuskersFile);
            
            // var conn = new HubConnectionBuilder()
            //     .WithUrl("http://localhost:5000/hubs/orchestrator")
            //     .WithConsoleLogger()
            //     .Build();

            // conn.StartAsync().Wait();
            // conn.InvokeAsync("test").Wait();

            // conn.StopAsync().Wait();
            // conn.DisposeAsync().Wait();

            // Console.WriteLine("Message sent.");
        }
    }
}
