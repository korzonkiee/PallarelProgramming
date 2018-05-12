namespace Shared.Messages
{
    public sealed class PerformancePermissionMessage : Message
    {
        public PerformancePermission PermissionToPerform { get; set; }

        public override string ToString()
        {
            return $"{nameof(PerformancePermissionMessage)}. Permission: {PermissionToPerform}";
        }
    }
}