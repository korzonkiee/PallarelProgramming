using System;
using System.IO;
using System.Linq;
using Busker;
using Shared;
using Xunit;

namespace BuskerUnitTests
{
    public class BuskerTests
    {
        private readonly BuskersLoader buskerLoader;

        public BuskerTests()
        {
            buskerLoader = new BuskersLoader();
        }

        [Fact]
        public void Test_BuskerLoader()
        {
            var path = $"../../../positions.txt";

            var buskers = buskerLoader.LoadBuskersFromFile(path);

            Assert.Equal(6, buskers.Count());
        }
    }
}
