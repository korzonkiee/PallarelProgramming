namespace Shared.Messages
{
    public class LooseMessage : Message
    {
        public override string ToString()
        {
            return $"{nameof(FinishedPerformanceMessage)}";
        }
    }
}