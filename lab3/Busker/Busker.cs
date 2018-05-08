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
        private Dictionary<int, Neighbour> neighbours;

        public int Value { get; }
        public Position Position { get; }
        public BuskerState State { get; } = BuskerState.Unknown;

        public int Id { get; }

        public Busker(int value, Position position)
        {
            Value = value;
            Position = position;

            Id = IdGenerator.GetId();
        }

        public void InitializeNeighbours(Dictionary<int, Neighbour> neighbours)
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

            connection.On(nameof(StartMessage), () => OnStart());
            connection.On(nameof(AcknowledgeMessage), (AcknowledgeMessage message) => OnAcknowledge(message));

            Console.WriteLine($"Busker {Id} enters city square.");

            await connection.InvokeAsync(nameof(ConnectMessage), new ConnectMessage()
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
                Value = this.Value,
                ReceiverIds = neighbours.Keys
            };

            return connection.InvokeAsync(nameof(AcknowledgeMessage), message);
        }

        private Task OnAcknowledge(AcknowledgeMessage message)
        {
            Console.WriteLine($"Busker {Id} received value: {message.Value}.");
            return Task.CompletedTask;
        }
    }
}