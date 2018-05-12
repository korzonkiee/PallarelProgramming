using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Shared.Messages;

namespace Orchestrator
{
    public class OrchestratorHub : Hub
    {
        private readonly Orchestrator orchestrator;
        public OrchestratorHub(Orchestrator orchestrator)
        {
            this.orchestrator = orchestrator;
        }

        [HubMethodName(nameof(Conn))]
        public Task Connect(Conn message)
        {
            return orchestrator.Connect(Context.ConnectionId, message);
        }

        [HubMethodName(nameof(Ack))]
        public Task Acknowledge(Ack message)
        {
            return orchestrator.SendMessage(nameof(Ack), message);
        }

        [HubMethodName(nameof(Loose))]
        public Task Status(Loose message)
        {
            return orchestrator.SendMessage(nameof(Loose), message);
        }

        [HubMethodName(nameof(End))]
        public Task FinishedPerformance(End message)
        {
            return orchestrator.SendMessage(nameof(End), message);
        }

        [HubMethodName(nameof(Perm))]
        public Task PerformancePermission(Perm message)
        {
            return orchestrator.SendMessage(nameof(Perm), message);
        }

        [HubMethodName(nameof(Req))]
        public Task RequestPerformance(Req message)
        {
            return orchestrator.SendMessage(nameof(Req), message);
        }
    }
}