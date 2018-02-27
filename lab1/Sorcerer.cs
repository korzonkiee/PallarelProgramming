using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace lab1
{
    public class Sorcerer
    {
        private readonly Random random;
        private readonly List<Factory> factories;

        private bool healingStopped;

        public Sorcerer(List<Factory> factories)
        {
            this.factories = factories;
            this.random = new Random();
        }

        public void StartHealing()
        {
            if (factories != null)
            {
                while (!healingStopped)
                {
                    int resourceTimeMs = random.Next(500, 1000);
                    Task.Delay(resourceTimeMs).Wait();

                    var randomFactoryIndex = random.Next(0, factories.Count);
                    var randomFactory = factories[randomFactoryIndex];

                    randomFactory.Heal();
                }
            }
        }

        public void StopHealing()
        {
            healingStopped = true;
        }
    }
}