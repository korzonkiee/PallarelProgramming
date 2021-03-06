using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Shared.Messages;

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
        public async Task Connect(string connectionId, Conn message)
        {
            try
            {
                await semaphore.WaitAsync();

                await hub.Groups.AddToGroupAsync(connectionId, message.SenderId.ToString());

                connectedBuskers++;

                if (connectedBuskers < numberOfBuskers)
                {
                    return;
                }
                else
                {
                    await hub.Clients.All.SendAsync(nameof(Conn));
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task SendMessage(string methodName, Message message)
        {
            foreach (var receiver in message.ReceiversIds)
            {
                await hub.Clients
                    .Group(receiver.ToString())
                    .SendAsync(methodName, message);
            }
        }
    }
}