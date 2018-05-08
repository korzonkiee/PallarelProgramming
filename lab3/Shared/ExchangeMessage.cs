namespace Shared
{
    public sealed class AcknowledgeMessage : Message
    {
        public int SenderId { get; set; }
        public int Value { get; set; }
    }
}