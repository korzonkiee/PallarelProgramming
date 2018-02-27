using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace lab1
{
    class Program
    {
        static SemaphoreSlim resourceCreatedSem = new SemaphoreSlim(0);

        static Random random = new Random();

        static void Main(string[] args)
        {
            var factories = CreateFactories();
            var indulger = new AlchemistsIndulger(resourceCreatedSem,
                factories[0], factories[1], factories[2]);

            StartAlchemistsIndulger(indulger);
            StartFactories(factories);

            SpawnWarlockAndSorcerer(factories);
            SpawnAlchemists(indulger);

            Task.Delay(20000).Wait();
        }

        private static List<Factory> CreateFactories()
        {
            return new List<Factory>
            {
                new Factory("Lead", resourceCreatedSem),
                new Factory("Sulfur", resourceCreatedSem),
                new Factory("Mercury", resourceCreatedSem)
            };
        }

        private static void StartFactories(List<Factory> factories)
        {
            foreach (var factory in factories)
            {
                Thread fThread = new Thread(() => factory.StartWorking());
                fThread.IsBackground = true;
                fThread.Start();
            }
        }

        private static void StartAlchemistsIndulger(AlchemistsIndulger indulger)
        {
            var iThread = new Thread(
                () => indulger.StartIndulging()
            );
            iThread.IsBackground = true;
            iThread.Start();
        }

        private static void SpawnAlchemists(AlchemistsIndulger indulger)
        {
            for (int i = 0; i < 100; i++)
            {
                int rand = random.Next(4);
                Alchemist alchemist = null;

                switch (rand)
                {
                    case 0:
                        alchemist = new AlchemistA();
                        break;
                    case 1:
                        alchemist = new AlchemistB();
                        break;
                    case 2:
                        alchemist = new AlchemistC();
                        break;
                    case 3:
                        alchemist = new AlchemistD();
                        break;
                }

                Thread thread = new Thread(() =>
                {
                    alchemist.RequestResources(indulger);
                });
                thread.IsBackground = true;
                thread.Start();
            }
        }

        private static void SpawnWarlockAndSorcerer(List<Factory> factories)
        {
            var warlock = new Warlock(factories);
            var sorcerer = new Sorcerer(factories);

            Thread wThread = new Thread(() => warlock.StartCursing());
            wThread.IsBackground = true;
            wThread.Start();

            Thread sThread = new Thread(() => sorcerer.StartHealing());
            sThread.IsBackground = true;
            sThread.Start();
        }
    }
}