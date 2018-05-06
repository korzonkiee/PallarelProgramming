using System;
using Microsoft.AspNetCore.SignalR;

namespace Orchestrator
{
    public class OrchestratorHub : Hub
    {
        public void Test()
        {
            Console.WriteLine("Test");
        }
    }
}