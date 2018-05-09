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

        private bool isPerforming = false;

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

            connection.On(nameof(StartMessage),
                () => OnStart());

            connection.On(nameof(AcknowledgeMessage),
                async (AcknowledgeMessage msg) => await OnAcknowledge(msg));

            connection.On(nameof(StateMessage),
                (StateMessage msg) => OnState(msg));

            connection.On(nameof(FinishedPerformanceMessage),
                (FinishedPerformanceMessage msg) => OnFinishedPerformance(msg));

            connection.On(nameof(RequestPerformanceMessage),
                (RequestPerformanceMessage msg) => OnRequestPerformance(msg));

            connection.On(nameof(PerformancePermissionMessage),
                (PerformancePermissionMessage msg) => OnPerformancePermissionMessage(msg));

            Console.WriteLine($"Busker {Id}, v: {Value} enters city square.");

            await connection.InvokeAsync(nameof(ConnectMessage), new ConnectMessage()
            {
                SenderId = Id
            });
        }

        private Task OnFinishedPerformance(FinishedPerformanceMessage msg)
        {
            if (State == BuskerState.Inactive)
                return Task.CompletedTask;

            Console.WriteLine($"Busker {Id} received {nameof(FinishedPerformanceMessage)} from {msg.SenderId}");
            if (State == BuskerState.Looser)
            {
                neighbours[msg.SenderId].State = BuskerState.Inactive;
                return RequestPerformance();
            }

            return Task.CompletedTask;
        }

        private Task OnRequestPerformance(RequestPerformanceMessage msg)
        {
            if (isPerforming)
                return RejectPerformanceRequest(msg.SenderId);

            return AcceptPerformanceRequest(msg.SenderId);
        }

        private Task OnPerformancePermissionMessage(PerformancePermissionMessage msg)
        {
            Console.WriteLine($"Busker {Id} received {nameof(PerformancePermissionMessage)}: {msg.PermissinoToPerform} from {msg.SenderId}");
            neighbours[msg.SenderId].PermissionToPerform = msg.PermissinoToPerform;

            if (DoNeighboursAgreeToPerform())
            {
                return Perform();
            }

            return Task.CompletedTask;

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

        private async Task OnAcknowledge(AcknowledgeMessage message)
        {
            Console.WriteLine($"Busker {Id} received value: {message.Value}.");

            // Assign received value to neighbour
            neighbours[message.SenderId].Value = message.Value;

            // Check if all neighbours are filled with value
            if (AreNeighboursFilledWithValue())
            {
                if (AmIWinner())
                {
                    await Perform();
                }
            }

            await SendIAmNoOne();
        }

        private Task OnState(StateMessage msg)
        {
            if (State == BuskerState.Inactive)
                return Task.CompletedTask;

            Console.WriteLine($"Busker {Id} received {msg.State.ToString()} from {msg.SenderId}.");
            if (msg.State == BuskerState.Winner)
            {
                Console.WriteLine($"Busker {Id} sets state as {nameof(BuskerState.Looser)}.");
                State = BuskerState.Looser;
            }

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

        private bool DoNeighboursAgreeToPerform()
        {
            foreach (var neighbour in neighbours.Values)
            {
                if (neighbour.PermissionToPerform == PerformancePermission.Rejected ||
                    neighbour.PermissionToPerform == PerformancePermission.NotSet)
                    return false;
            }

            return true;
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

        private Task SendIAmWinner()
        {
            var msg = new StateMessage()
            {
                State = BuskerState.Winner,
                ReceiversIds = neighbours.Keys,
                SenderId = Id
            };

            return connection.InvokeAsync(nameof(StateMessage), msg);
        }

        private Task SendIAmNoOne()
        {
            var msg = new StateMessage()
            {
                State = BuskerState.Unknown,
                ReceiversIds = neighbours.Keys,
                SenderId = Id
            };

            return connection.InvokeAsync(nameof(StateMessage), msg);
        }

        private Task SendFinishedPerforming()
        {
            var msg = new FinishedPerformanceMessage()
            {
                SenderId = Id,
                ReceiversIds = neighbours.Keys
            };

            return connection.InvokeAsync(nameof(FinishedPerformanceMessage), msg);
        }

        private Task RequestPerformance()
        {
            var msg = new RequestPerformanceMessage()
            {
                SenderId = Id,
                ReceiversIds = neighbours.Keys
            };

            return connection.InvokeAsync(nameof(RequestPerformanceMessage), msg);
        }

        private async Task Perform()
        {
            Console.WriteLine($"Busker {Id} is winner and stats performing.");

            State = BuskerState.Winner;

            await SendIAmWinner();

            isPerforming = true;
            await Task.Delay(2000);
            isPerforming = false;

            Console.WriteLine($"Busker {Id} becomes {nameof(BuskerState.Inactive)}.");

            State = BuskerState.Inactive;
            await SendFinishedPerforming();
        }

        private Task RejectPerformanceRequest(int requestFrom)
        {
            Console.WriteLine($"Busker {Id} rejects performance request from {requestFrom}");

            var msg = new PerformancePermissionMessage()
            {
                PermissinoToPerform = PerformancePermission.Rejected,
                ReceiversIds = new List<int>() { requestFrom },
                SenderId = Id
            };

            return connection.InvokeAsync(nameof(PerformancePermissionMessage), msg);
        }

        private Task AcceptPerformanceRequest(int requestFrom)
        {
            Console.WriteLine($"Busker {Id} accepts performance request from {requestFrom}");

            var msg = new PerformancePermissionMessage()
            {
                PermissinoToPerform = PerformancePermission.Accepted,
                ReceiversIds = new List<int>() { requestFrom },
                SenderId = Id
            };

            return connection.InvokeAsync(nameof(PerformancePermissionMessage), msg);
        }
    }
}