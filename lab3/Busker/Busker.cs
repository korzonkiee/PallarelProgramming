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

            stateMachine.OnUnhandledTrigger((s, t) =>
            {
                Console.WriteLine($"Unhandled exception in state {s} by trigger {t}");
            });

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
                .PermitReentry(Trigger.Notify)
                .Permit(Trigger.Win, State.Winner)
                .Permit(Trigger.Loose, State.Looser)
                .InternalTransition(Trigger.Rst, AckNeighs)
                .InternalTransition<Req>(requestPerformanceTrigger, OnRequest)
                .InternalTransition<Ack>(acknowledgeTrigger, OnAcknowledgeNeighbour)
                .InternalTransition<Perm>(performancePermissionTrigger, OnPerformancePermission)
                .InternalTransition<End>(finishedPerformanceTrigger, OnFinishedPerformance);

            stateMachine.Configure(State.Looser)
                .OnEntry(NotifyNeighs)
                .Permit(Trigger.Rst, State.Unknown)
                .Ignore(Trigger.Notify)
                .Ignore(Trigger.Loose)
                .InternalTransition<Ack>(acknowledgeTrigger, SendBackDecreasedValue)
                .InternalTransition<Req>(requestPerformanceTrigger, OnRequest)
                .InternalTransition<End>(finishedPerformanceTrigger, OnFinishedPerformance);

            // Configure Winner state.
            stateMachine.Configure(State.Winner)
                .OnEntry(LooseToNeighs)
                .OnEntry(Perform)
                .Permit(Trigger.End, State.Inactive)
                .Ignore(Trigger.Notify)
                .OnExit(EndToNeighs)
                .InternalTransition<Req>(requestPerformanceTrigger, OnRequest);

            // Configure Inactive state.
            stateMachine.Configure(State.Inactive)
                .Ignore(Trigger.Notify)
                .Ignore(Trigger.Ack)
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

            connection.On(nameof(Notify),
                (Notify msg) => stateMachine.Fire(Trigger.Notify));
        }

        private void AckNeighs()
        {
            if (neighbours.Values.Count == 0)
                stateMachine.Fire(Trigger.Win);

            var msg = new Ack()
            {
                Value = this.Value,
                SenderId = Id,
                ReceiversIds = neighbours.Keys
            };

            connection.InvokeAsync(nameof(Ack), msg);
        }

        private void NotifyNeighs()
        {
            var msg = new Notify()
            {
                SenderId = Id,
                ReceiversIds = neighbours.Keys
            };

            connection.InvokeAsync(nameof(Notify), msg);
        }

        // When looser receives AcknowledgeMessage we response to sender
        // with our Value decreased by one so that we give him a chance to
        // become a winner.
        private void SendBackDecreasedValue(Ack msg, Transition tran)
        {
            var res = new Ack()
            {
                Value = msg.Value - 1,
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

        private void Perform()
        {
            Task.Delay(2000).Wait();
            stateMachine.Fire<End>(finishedPerformanceTrigger, null);
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
        }

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