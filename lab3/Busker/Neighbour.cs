using System;

namespace Busker
{
    public class Neighbour
    {
        public int Id { get; }
        public int Value { get; }
        public Neighbour(int id, int value)
        {
            Id = id;
            Value = value;
        }
    }
}