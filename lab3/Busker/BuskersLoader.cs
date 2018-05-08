using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shared;

namespace Busker
{
    public class BuskersLoader
    {
        private readonly Random random = new Random();

        public Busker[] LoadBuskersFromFile(string path)
        {
            using (StreamReader file = new StreamReader(path))
            {
                string header = file.ReadLine();

                int numberOfBuskers = int.Parse(header);
                int priorityUpperBound = (int) Math.Pow(numberOfBuskers, 4);
                
                var buskers = InitializeBuskers(file, numberOfBuskers, priorityUpperBound);
                AssignNeighbours(buskers);

                return buskers;
            }
        }

        private Busker[] InitializeBuskers(StreamReader file, int numberOfBuskers, int priorityUpperBound)
        {
            var buskers = new Busker[numberOfBuskers];
            for (int i = 0; i < numberOfBuskers; i++)
            {
                string buskerLine = file.ReadLine();
                string[] buskerPos = buskerLine.Split(" ", 2);

                int x = int.Parse(buskerPos[0]);
                int y = int.Parse(buskerPos[1]);

                int id = (int) random.Next(priorityUpperBound);
                Position pos = new Position(x, y);

                buskers[i] = new Busker(id.ToString(), pos);
            }

            return buskers;
        }

        private void AssignNeighbours(Busker[] buskers)
        {
            for (int i = 0 ; i < buskers.Length; i++)
            {
                var neighbours = buskers
                    .Where(b => b != buskers[i] && b.Position.DistanceTo(buskers[i].Position) <= Config.HearingRange)
                    .Select(b => new Neighbour(b.Id))
                    .ToDictionary(b => b.Id);

                buskers[i].InitializeNeighbours(neighbours);
            }
        }
    }
}