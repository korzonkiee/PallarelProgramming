using System;
using System.Collections.Generic;

namespace Shared
{
    public abstract class Message
    {
        public IEnumerable<int> ReceiversIds { get; set; }
    }
}