using System.Collections.Generic;

namespace Shared
{
    public abstract class Message
    {
        public IEnumerable<string> ReceiverIds { get; set; }
    }
}