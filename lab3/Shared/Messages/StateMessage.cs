namespace Shared
{
    public sealed class StateMessage : Message
    {
        public BuskerState State { get; set; }
    }
}