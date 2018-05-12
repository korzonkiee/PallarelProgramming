using System;

namespace Shared.Messages
{
    public sealed class Conn : Message
    {
        public override string ToString()
        {
            return $"{nameof(Conn)}";
        }
    }
}