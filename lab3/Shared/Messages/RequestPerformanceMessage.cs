namespace Shared.Messages
{
    public sealed class RequestPerformanceMessage : Message
    {
        public override string ToString()
        {
            return $"{nameof(RequestPerformanceMessage)}";
        }
    }
}