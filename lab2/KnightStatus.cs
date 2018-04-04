using System;

namespace monitors
{
    [Flags]
    public enum KnightStatus
    {
        NotTalking = 0,
        WaitingForNeigh = 1 << 0,
        WaitingForKing = 1 << 1,
        ListeningToKing = 1 << 2,
        Talking = 1 << 3
    }
}