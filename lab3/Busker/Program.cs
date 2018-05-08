using System;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.SignalR.Client;
using Shared;

namespace Busker
{
    class Program
    {
        static void Main(string[] args)
        {
            var buskersLoader = new BuskersLoader();
            var buskers = buskersLoader
                .LoadBuskersFromFile(Files.BuskersFile);

            var buskersThreads = new Thread[buskers.Length];
            for (int i = 0; i < buskers.Length; i++)
            {
                int _i = i;
                buskersThreads[_i] = new Thread(async () =>
                {
                    await buskers[_i].EnterCitySquare();
                });

                buskersThreads[_i].Start();
            }

            Console.Read();
        }
    }
}
