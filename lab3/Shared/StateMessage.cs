namespace Shared
{
    public class StateMessage : Message
    {
        public int SenderId { get; set; }
        public BuskerState State { get; set; }
    }
}