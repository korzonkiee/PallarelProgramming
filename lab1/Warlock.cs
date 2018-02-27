using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace lab1
{
    public class Warlock
    {
        private readonly Random random;
        private readonly List<Factory> factories;

        private bool curseStopped;

        public Warlock(List<Factory> factories)
        {
            this.factories = factories;
            this.random = new Random();
        }

        public void StartCursing()
        {
            if (factories != null)
            {
                while (!curseStopped)
                {
                    int resourceTimeMs = random.Next(500, 1000);
                    Task.Delay(resourceTimeMs).Wait();

                    var randomFactoryIndex = random.Next(0, factories.Count);
                    var randomFactory = factories[randomFactoryIndex];

                    randomFactory.Curse();
                }
            }
        }

        public void StopCursing()
        {
            curseStopped = true;
        }
    }
}