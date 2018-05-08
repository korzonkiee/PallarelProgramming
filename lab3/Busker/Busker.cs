using Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Busker
{
    public class Busker
    {
        private HubConnection connection;
        private Dictionary<string, Neighbour> neighbours;

        public string Id { get; }
        public Position Position { get; }
        public BuskerState State { get; } = BuskerState.Unknown;

        public Busker(string id, Position position)
        {
            Id = id;
            Position = position;
        }

        public void InitializeNeighbours(Dictionary<string, Neighbour> neighbours)
        {
            this.neighbours = neighbours;
        }

        public async Task EnterCitySquare()
        {
            connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/hubs/orchestrator")
                .Build();

            connection.Closed += OnClosed;

            await connection.StartAsync();

            connection.On("start", () => OnStart());
            connection.On("exchange", (AcknowledgeMessage message) => OnExchange(message));

            await connection.InvokeAsync("connect", new ConnectMessage()
            {
                SenderId = Id
            });
        }

        private Task OnClosed(Exception arg)
        {
            Console.WriteLine(arg.Message);
            return connection.DisposeAsync();
        }

        private Task OnStart()
        {
            var message = new AcknowledgeMessage()
            {
                V = Id,
                ReceiverIds = neighbours.Keys
            };

            return connection.InvokeAsync("exchange", message);
        }

        private Task OnExchange(AcknowledgeMessage message)
        {
            return Task.CompletedTask;
        }
    }
}