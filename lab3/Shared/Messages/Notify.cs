namespace Shared.Messages
{
    public sealed class Notify : Message
    {
        public override string ToString()
        {
            return $"{nameof(Notify)}";
        }
    }
}