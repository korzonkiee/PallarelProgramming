using System;

namespace Shared.Messages
{
    public sealed class ConnectMessage : Message
    {
        public override string ToString()
        {
            return $"{nameof(AcknowledgeMessage)}";
        }
    }
}