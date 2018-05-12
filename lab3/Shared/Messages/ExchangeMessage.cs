using System;

namespace Shared.Messages
{
    public sealed class AcknowledgeMessage : Message
    {
        public int Value { get; set; }

        public override string ToString()
        {
            return $"{nameof(AcknowledgeMessage)}. Value: {Value}";
        }
    }
}