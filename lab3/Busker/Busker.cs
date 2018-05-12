using Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Stateless;
using Shared.Messages;
using Stateless.Graph;

using State = Shared.State;

namespace Busker
{
    using static Stateless.StateMachine<State, Trigger>;

    public class Busker
    {
        private object ackObj = new object();

        private HubConnection connection;
        private Dictionary<int, Neighbour> neighbours;

        public int Value { get; }
        public Position Position { get; }
        public int Stage { get; private set; } = 0;
        // public BuskerState State { get; private set; } = BuskerState.Unknown;

        private bool isPerforming = false;

        public int Id { get; }

        private StateMachine<State, Trigger> stateMachine;
        private TriggerWithParameters<Ack> acknowledgeTrigger;
        private TriggerWithParameters<Perm> performancePermissionTrigger;
        private TriggerWithParameters<Req> requestPerformanceTrigger;
        private TriggerWithParameters<End> finishedPerformanceTrigger;

        public Busker(int value, Position position)
        {
            Value = value;
            Position = position;

            Id = IdGenerator.GetId();

            stateMachine = BuildStateMachine();
            connection = BuildConnection();
        }

        private StateMachine<State, Trigger> BuildStateMachine()
        {
            // At the begging every Busker is in Disconnected state.
            var stateMachine = new StateMachine<State, Trigger>(State.Disconnected);

            // Add logging between state transitions.
            stateMachine.OnTransitioned(trans => Logger.LogTransition(this, trans));

            // Create parametrised triggers.
            acknowledgeTrigger = stateMachine
                .SetTriggerParameters<Ack>(Trigger.Ack);
            performancePermissionTrigger = stateMachine
                .SetTriggerParameters<Perm>(Trigger.Perm);
            requestPerformanceTrigger = stateMachine
                .SetTriggerParameters<Req>(Trigger.Req);
            finishedPerformanceTrigger = stateMachine
                .SetTriggerParameters<End>(Trigger.End);

            // Configure Disconntected state.
            stateMachine.Configure(State.Disconnected)
                .Permit(Trigger.Conn, State.Unknown);

            // Configure Unknown state.
            stateMachine.Configure(State.Unknown)
                .OnEntry(UpdateStage)
                .OnEntry(AckNeighs)
                .Permit(Trigger.Win, State.Winner)
                .Permit(Trigger.Loose, State.Looser)
                .InternalTransition<Req>(requestPerformanceTrigger, OnRequest)
                .InternalTransition<Ack>(acknowledgeTrigger, OnAcknowledgeNeighbour)
                .InternalTransition<Perm>(performancePermissionTrigger, OnPerformancePermission);

            stateMachine.Configure(State.Looser)
                .Permit(Trigger.Rst, State.Unknown)
                .InternalTransition<Ack>(acknowledgeTrigger, SendBackDecreasedValue)
                .InternalTransition<Req>(requestPerformanceTrigger, OnRequest)
                .InternalTransition<End>(finishedPerformanceTrigger, OnFinishedPerformance);

            // Configure Winner state.
            stateMachine.Configure(State.Winner)
                .OnEntry(LooseToNeighs)
                .OnEntryAsync(Perform)
                .Permit(Trigger.End, State.Inactive)
                .OnExit(EndToNeighs)
                .InternalTransition<Req>(requestPerformanceTrigger, OnRequest);

            // Configure Inactive state.
            stateMachine.Configure(State.Inactive)
                .InternalTransition<Req>(requestPerformanceTrigger, OnRequest);

            return stateMachine;
        }


        private Task Connect()
        {
            return connection.InvokeAsync(nameof(Conn), new Conn()
            {
                SenderId = Id
            });
        }

        private void SetUpMessageHandlers()
        {
            connection.On(nameof(Conn),
                () => stateMachine.Fire(Trigger.Conn));

            connection.On(nameof(Ack),
                (Ack msg) => stateMachine.Fire<Ack>(acknowledgeTrigger, msg));

            connection.On(nameof(Loose),
                (Loose msg) => stateMachine.Fire(Trigger.Loose));

            connection.On(nameof(Perm),
                (Perm msg) => stateMachine.Fire<Perm>(performancePermissionTrigger, msg));

            connection.On(nameof(Req),
                (Req msg) => stateMachine.Fire<Req>(requestPerformanceTrigger, msg));

            connection.On(nameof(End),
                (End msg) => stateMachine.Fire<End>(finishedPerformanceTrigger, msg));
        }

        private void AckNeighs()
        {
            var message = new Ack()
            {
                Value = this.Value,
                SenderId = Id,
                ReceiversIds = neighbours.Keys
            };

            connection.InvokeAsync(nameof(Ack), message);
        }

        // When looser receives AcknowledgeMessage we response to sender
        // with our Value decreased by one so that we give him a chance to
        // become a winner.
        private void SendBackDecreasedValue(Ack msg, Transition tran)
        {
            var res = new Ack()
            {
                Value = this.Value - 1,
                SenderId = this.Id,
                ReceiversIds = new List<int>() { msg.SenderId }
            };

            connection.InvokeAsync(nameof(Ack), res);
        }

        private void OnFinishedPerformance(End msg, Transition trans)
        {
            // Remove finished busker from neighbours.
            neighbours.Remove(msg.SenderId);

            stateMachine.Fire(Trigger.Rst);
        }

        private void UpdateStage()
        {
            Stage++;
            ResetNeighbours();
        }

        private void OnRequest(Req msg, Transition trans)
        {
            PerformancePermission permission;

            switch (stateMachine.State)
            {
                case State.Unknown:
                case State.Looser:
                case State.Inactive:
                    permission = PerformancePermission.Accepted;
                    break;
                case State.Winner:
                    permission = PerformancePermission.Rejected;
                    break;
                default:
                    permission = PerformancePermission.Rejected;
                    break;
            }

            var decision = new Perm()
            {
                SenderId = Id,
                ReceiversIds = new List<int>() { msg.SenderId },
                PermissionToPerform = permission
            };

            connection.InvokeAsync(nameof(Perm), decision);
        }

        private void OnAcknowledgeNeighbour(Ack msg, Transition trans)
        {
            Logger.Log(this, msg);
            AssignReceivedValueToNeighbour(msg.SenderId, msg.Value);

            // If it was the last neighbour and I have the greatest value
            // then send Request to all neighs
            if (AreAllNeighboursAcknowledged() && AmIWinner())
                RequestNeighboursForPerformance();
        }

        private void OnPerformancePermission(Perm msg, Transition trans)
        {
            Logger.Log(this, msg);
            AssignReceivedPermissionToNeighbour(msg.SenderId, msg.PermissionToPerform);

            if (DoAllNeighboursLetMePeform())
                stateMachine.Fire(Trigger.Win);

        }

        private void LooseToNeighs()
        {
            var msg = new Loose()
            {
                ReceiversIds = neighbours.Keys,
                SenderId = Id
            };

            connection.InvokeAsync(nameof(Loose), msg);
        }

        private void EndToNeighs()
        {
            var msg = new End()
            {
                SenderId = Id,
                ReceiversIds = neighbours.Keys
            };

            connection.InvokeAsync(nameof(End), msg);
        }

        private async Task Perform()
        {
            await Task.Delay(2000);
            stateMachine.Fire(Trigger.End);
        }

        private void AssignReceivedValueToNeighbour(int neighbourId, int value)
        {
            neighbours[neighbourId].Value = value;
        }

        private void AssignReceivedPermissionToNeighbour(int neighbourId, PerformancePermission permission)
        {
            neighbours[neighbourId].PermissionToPerform = permission;
        }

        private bool AreAllNeighboursAcknowledged()
        {
            foreach (var neighbour in neighbours.Values)
            {
                if (!neighbour.Value.HasValue)
                    return false;
            }

            return true;
        }

        private bool DoAllNeighboursLetMePeform()
        {
            foreach (var neighbour in neighbours.Values)
            {
                if (neighbour.PermissionToPerform == PerformancePermission.NotSet ||
                    neighbour.PermissionToPerform == PerformancePermission.Rejected)
                    return false;
            }

            return true;
        }

        private bool AmIWinner()
        {
            foreach (var neighbour in neighbours.Values)
            {
                if (this.Value < neighbour.Value.Value)
                    return false;
            }

            return true;
        }

        private void RequestNeighboursForPerformance()
        {
            Console.WriteLine($"Busker {Id} knows his neighbours. Requests will be sent.");

            var msg = new Req()
            {
                SenderId = Id,
                ReceiversIds = neighbours.Keys
            };

            connection.InvokeAsync(nameof(Req), msg);
        }

        public void InitializeNeighbours(Dictionary<int, Neighbour> neighbours)
        {
            this.neighbours = neighbours;
        }

        public async Task EnterCitySquare()
        {
            await connection.StartAsync();
            SetUpMessageHandlers();
            await Connect();

            // connection.On(nameof(AcknowledgeMessage),
            //     async (AcknowledgeMessage msg) => await OnAcknowledge(msg));

            // connection.On(nameof(StateMessage),
            //     (StateMessage msg) => OnState(msg));

            // connection.On(nameof(FinishedPerformanceMessage),
            //     (FinishedPerformanceMessage msg) => OnFinishedPerformance(msg));

            // connection.On(nameof(RequestPerformanceMessage),
            //     (RequestPerformanceMessage msg) => OnRequestPerformance(msg));

            // connection.On(nameof(PerformancePermissionMessage),
            //     (PerformancePermissionMessage msg) => OnPerformancePermissionMessage(msg));

            // Console.WriteLine($"Busker {Id}, v: {Value} enters city square.");
        }

        // private Task OnFinishedPerformance(FinishedPerformanceMessage msg)
        // {
        //     if (State == BuskerState.Inactive)
        //         return Task.CompletedTask;

        //     Console.WriteLine($"Busker {Id} received {nameof(FinishedPerformanceMessage)} from {msg.SenderId}");
        //     if (State == BuskerState.Looser)
        //     {
        //         neighbours[msg.SenderId].State = BuskerState.Inactive;
        //         return RequestPerformance();
        //     }

        //     return Task.CompletedTask;
        // }

        // private Task OnRequestPerformance(RequestPerformanceMessage msg)
        // {
        //     if (isPerforming)
        //         return RejectPerformanceRequest(msg.SenderId);

        //     return AcceptPerformanceRequest(msg.SenderId);
        // }

        // private Task OnPerformancePermissionMessage(PerformancePermissionMessage msg)
        // {
        //     Console.WriteLine($"Busker {Id} received {nameof(PerformancePermissionMessage)}: {msg.PermissinoToPerform} from {msg.SenderId}");
        //     neighbours[msg.SenderId].PermissionToPerform = msg.PermissinoToPerform;

        //     if (DoNeighboursAgreeToPerform())
        //     {
        //         return Perform();
        //     }

        //     return Task.CompletedTask;

        // }

        // private Task OnClosed(Exception arg)
        // {
        //     Console.WriteLine(arg.Message);
        //     return connection.DisposeAsync();
        // }

        // private async Task OnAcknowledge(AcknowledgeMessage message)
        // {
        //     Console.WriteLine($"Busker {Id} received value: {message.Value}.");

        //     // Assign received value to neighbour
        //     neighbours[message.SenderId].Value = message.Value;

        //     // Check if all neighbours are filled with value
        //     if (AreNeighboursFilledWithValue())
        //     {
        //         if (AmIWinner())
        //         {
        //             await Perform();
        //         }
        //     }

        //     await SendIAmNoOne();
        // }

        // private Task OnState(StateMessage msg)
        // {
        //     if (State == BuskerState.Inactive)
        //         return Task.CompletedTask;

        //     Console.WriteLine($"Busker {Id} received {msg.State.ToString()} from {msg.SenderId}.");
        //     if (msg.State == BuskerState.Winner)
        //     {
        //         Console.WriteLine($"Busker {Id} sets state as {nameof(BuskerState.Looser)}.");
        //         State = BuskerState.Looser;
        //     }

        //     return Task.CompletedTask;
        // }

        // private bool AreNeighboursFilledWithValue()
        // {
        //     bool areFilled = true;
        //     foreach (var neighbour in neighbours.Values)
        //     {
        //         if (!neighbour.Value.HasValue)
        //             return false;
        //     }
        //     return areFilled;
        // }

        // private bool DoNeighboursAgreeToPerform()
        // {
        //     foreach (var neighbour in neighbours.Values)
        //     {
        //         if (neighbour.PermissionToPerform == PerformancePermission.Rejected ||
        //             neighbour.PermissionToPerform == PerformancePermission.NotSet)
        //             return false;
        //     }

        //     return true;
        // }

        // private bool AmIWinner()
        // {
        //     bool amIWinner = true;
        //     foreach (var neighbour in neighbours.Values)
        //     {
        //         if (this.Value < neighbour.Value.Value)
        //             return false;
        //     }
        //     return amIWinner;
        // }

        // private Task SendIAmWinner()
        // {
        //     var msg = new StateMessage()
        //     {
        //         State = BuskerState.Winner,
        //         ReceiversIds = neighbours.Keys,
        //         SenderId = Id
        //     };

        //     return connection.InvokeAsync(nameof(StateMessage), msg);
        // }

        // private Task SendIAmNoOne()
        // {
        //     var msg = new StateMessage()
        //     {
        //         State = BuskerState.Unknown,
        //         ReceiversIds = neighbours.Keys,
        //         SenderId = Id
        //     };

        //     return connection.InvokeAsync(nameof(StateMessage), msg);
        // }

        // private Task SendFinishedPerforming()
        // {
        //     var msg = new FinishedPerformanceMessage()
        //     {
        //         SenderId = Id,
        //         ReceiversIds = neighbours.Keys
        //     };

        //     return connection.InvokeAsync(nameof(FinishedPerformanceMessage), msg);
        // }

        // private Task RequestPerformance()
        // {
        //     var msg = new RequestPerformanceMessage()
        //     {
        //         SenderId = Id,
        //         ReceiversIds = neighbours.Keys
        //     };

        //     return connection.InvokeAsync(nameof(RequestPerformanceMessage), msg);
        // }

        // private async Task Perform()
        // {
        //     Console.WriteLine($"Busker {Id} is winner and stats performing.");

        //     State = BuskerState.Winner;

        //     await SendIAmWinner();

        //     isPerforming = true;
        //     await Task.Delay(2000);
        //     isPerforming = false;

        //     Console.WriteLine($"Busker {Id} becomes {nameof(BuskerState.Inactive)}.");

        //     State = BuskerState.Inactive;
        //     await SendFinishedPerforming();
        // }

        // private Task RejectPerformanceRequest(int requestFrom)
        // {
        //     Console.WriteLine($"Busker {Id} rejects performance request from {requestFrom}");

        //     var msg = new PerformancePermissionMessage()
        //     {
        //         PermissinoToPerform = PerformancePermission.Rejected,
        //         ReceiversIds = new List<int>() { requestFrom },
        //         SenderId = Id
        //     };

        //     return connection.InvokeAsync(nameof(PerformancePermissionMessage), msg);
        // }

        // private Task AcceptPerformanceRequest(int requestFrom)
        // {
        //     Console.WriteLine($"Busker {Id} accepts performance request from {requestFrom}");

        //     var msg = new PerformancePermissionMessage()
        //     {
        //         PermissinoToPerform = PerformancePermission.Accepted,
        //         ReceiversIds = new List<int>() { requestFrom },
        //         SenderId = Id
        //     };

        //     return connection.InvokeAsync(nameof(PerformancePermissionMessage), msg);
        // }

        public string GetGraph()
        {
            return UmlDotGraph.Format(stateMachine.GetInfo());
        }

        private void ResetNeighbours()
        {
            foreach (var neighbour in neighbours.Values)
            {
                neighbour.Reset();
            }
        }

        private HubConnection BuildConnection()
        {
            return new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/hubs/orchestrator")
                .Build();
        }

        public override string ToString()
        {
            return $"Busker (Id: {Id}, V: {Value})";
        }
    }
}