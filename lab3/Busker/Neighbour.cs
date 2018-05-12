using System;
using Shared;

namespace Busker
{
    public class Neighbour
    {
        public int Id { get; }
        public int? Value { get; set; }

        public BuskerState State { get; set; }
        public PerformancePermission PermissionToPerform { get; set; }

        public Neighbour(int id)
        {
            Id = id;
        }

        public void Reset()
        {
            Value = null;
            State = BuskerState.Unknown;
            PermissionToPerform = PerformancePermission.NotSet;
        }
    }
}