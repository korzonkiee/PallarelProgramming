using System;

namespace Shared
{
    public sealed class ConnectMessage : Message
    {
        public int SenderId { get; set; }
    }
}