using System;

namespace Shared.Messages
{
    public sealed class Ack : Message
    {
        public int Value { get; set; }

        public override string ToString()
        {
            return $"{nameof(Ack)}. Value: {Value}";
        }
    }
}