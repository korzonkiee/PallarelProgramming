using System.Collections.Generic;
using System.IO;
using Shared;

namespace Busker
{
    public class BuskersLoader
    {
        public IEnumerable<Busker> LoadBuskersFromFile(string path)
        {
            using (StreamReader file = new StreamReader(path))
            {
                string header = file.ReadLine();

                int numberOfBuskers = int.Parse(header);

                var buskers = new Busker[numberOfBuskers];
                for (int i = 0; i < numberOfBuskers; i++)
                {
                    string buskerLine = file.ReadLine();
                    string[] buskerPos = buskerLine.Split(" ", 2);

                    int x = int.Parse(buskerPos[0]);
                    int y = int.Parse(buskerPos[1]);

                    var pos = new Position(x, y);

                    buskers[i] = new Busker(pos);
                }

                return buskers;
            }
        }
    }
}