using System;
using System.Collections.Generic;

namespace Shared
{
    public abstract class Message
    {
        public int SenderId { get; set; }
        public IEnumerable<int> ReceiversIds { get; set; }
    }
}