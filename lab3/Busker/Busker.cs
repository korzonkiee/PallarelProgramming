using Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace Busker
{
    public class Busker
    {
        private object ackObj = new object();

        private HubConnection connection;
        private Dictionary<int, Neighbour> neighbours;

        public int Value { get; }
        public Position Position { get; }
        public BuskerState State { get; private set; } = BuskerState.Unknown;

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
            connection.On(nameof(AcknowledgeMessage), (AcknowledgeMessage msg) => OnAcknowledge(msg));
            connection.On(nameof(StateMessage), (StateMessage msg) => OnState(msg));

            Console.WriteLine($"Busker {Id}, v: {Value} enters city square.");

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
                SenderId = Id,
                ReceiversIds = neighbours.Keys
            };

            return connection.InvokeAsync(nameof(AcknowledgeMessage), message);
        }

        private Task OnAcknowledge(AcknowledgeMessage message)
        {
            lock (ackObj)
            {
                Console.WriteLine($"Busker {Id} received value: {message.Value}.");

                // Assign received value to neighbour
                neighbours[message.SenderId].Value = message.Value;

                // Check if all neighbours are filled with value
                if (AreNeighboursFilledWithValue())
                {
                    if (AmIWinner())
                    {
                        Console.WriteLine($"Busker {Id} is winner.");

                        State = BuskerState.Winner;
                        return SendLooserMessagesToNeighbours();
                    }
                }

                return Task.CompletedTask;
            }
        }

        private Task OnState(StateMessage msg)
        {
            Console.WriteLine($"Busker {Id} received {msg.State.ToString()} from {msg.SenderId}.");
            return Task.CompletedTask;
        }

        private bool AreNeighboursFilledWithValue()
        {
            bool areFilled = true;
            foreach (var neighbour in neighbours.Values)
            {
                if (!neighbour.Value.HasValue)
                    return false;
            }
            return areFilled;
        }

        private bool AmIWinner()
        {
            bool amIWinner = true;
            foreach (var neighbour in neighbours.Values)
            {
                if (this.Value < neighbour.Value.Value)
                    return false;
            }
            return amIWinner;
        }

        private Task SendLooserMessagesToNeighbours()
        {
            var msg = new StateMessage()
            {
                State = BuskerState.Looser,
                ReceiversIds = neighbours.Keys
            };

            return connection.InvokeAsync(nameof(StateMessage), msg);
        }
    }
}