namespace Shared.Messages
{
    public sealed class Req : Message
    {
        public override string ToString()
        {
            return $"{nameof(Req)}";
        }
    }
}