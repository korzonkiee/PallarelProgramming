namespace Shared.Messages
{
    public sealed class End : Message
    {
        public override string ToString()
        {
            return $"{nameof(End)}";
        }
    }
}