using System.IO;
using System.Linq;
using Shared;

namespace Orchestrator
{
    public static class BuskersLoader
    {
        public static int GetNumberOfMusicians()
        {
            var data = File.ReadLines(Files.BuskersFile).First();
            return int.Parse(data);
        }
    }
}