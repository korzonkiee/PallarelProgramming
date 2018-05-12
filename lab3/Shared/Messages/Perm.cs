namespace Shared.Messages
{
    public sealed class Perm : Message
    {
        public PerformancePermission PermissionToPerform { get; set; }

        public override string ToString()
        {
            return $"{nameof(Perm)}. Permission: {PermissionToPerform}";
        }
    }
}