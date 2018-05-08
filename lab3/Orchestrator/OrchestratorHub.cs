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

        public Task Connect(ConnectMessage message)
        {
            return orchestrator.Connect(Context.ConnectionId, message);
        }

        public Task Exchange(ExchangeMessage message)
        {
            return orchestrator.SendMessage("exchange", message);
        }
    }
}