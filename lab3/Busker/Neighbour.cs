using System;

namespace Busker
{
    public class Neighbour
    {
        public int Id { get; }
        public int? Value { get; set; }

        public Neighbour(int id)
        {
            Id = id;
        }
    }
}