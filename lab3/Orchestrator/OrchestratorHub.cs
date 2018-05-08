using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Shared;

namespace Orchestrator
{
    public class OrchestratorHub : Hub
    {
        private readonly Orchestrator orchestrator;
        public OrchestratorHub(Orchestrator orchestrator)
        {
            this.orchestrator = orchestrator;
        }

        [HubMethodName(nameof(ConnectMessage))]
        public Task Connect(ConnectMessage message)
        {
            return orchestrator.Connect(Context.ConnectionId, message);
        }

        [HubMethodName(nameof(AcknowledgeMessage))]
        public Task Acknowledge(AcknowledgeMessage message)
        {
            return orchestrator.SendMessage(nameof(AcknowledgeMessage), message);
        }

        [HubMethodName(nameof(StateMessage))]
        public Task Status(StateMessage message)
        {
            return orchestrator.SendMessage(nameof(StateMessage), message);
        }
    }
}