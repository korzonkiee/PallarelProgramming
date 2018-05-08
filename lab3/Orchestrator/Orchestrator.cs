using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Shared;

namespace Orchestrator
{
    public sealed class Orchestrator
    {
        private readonly IHubContext<OrchestratorHub> hub;
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private readonly int numberOfBuskers;
        private int connectedBuskers = 0;
        

        public Orchestrator(IHubContext<OrchestratorHub> hub)
        {
            this.hub = hub;
            this.numberOfBuskers = BuskersLoader.GetNumberOfMusicians();
        }
        public async Task Connect(string connectionId, ConnectMessage message)
        {
            try
            {
                await semaphore.WaitAsync();

                await hub.Groups.AddAsync(connectionId, message.SenderId);

                connectedBuskers++;

                if (connectedBuskers < numberOfBuskers)
                {
                    return;
                }
                else
                {
                    await hub.Clients.All.SendAsync("start");
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task SendMessage(string methodName, Message message)
        {
            foreach (var receiver in message.ReceiverIds)
            {
                await hub.Clients.Group(receiver).SendAsync("exchange", message);
            }
        }
    }
}