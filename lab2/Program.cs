using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace monitors
{
    class Program
    {
        static void Main(string[] args)
        {
            Rostrum rostrum = new Rostrum();
            DrinkingBout drinkingBout = new DrinkingBout();

            Knight[] knights = InitializeKnights(rostrum, drinkingBout);
            rostrum.RegisterKnights(knights);


            var threads = StartParty(knights, drinkingBout);
            StopParty(threads.Item1, threads.Item2);
        }

        private static Knight[] InitializeKnights(Rostrum rostrum, DrinkingBout drinkingBout)
        {
            var knights = new Knight[Config.NumberOfKnights];

            for (int i = 1; i < Config.NumberOfKnights - 1; i++)
            {
                knights[i] = new Knight(i, i + 1, i - 1, rostrum, drinkingBout);
            }
            knights[0] = new Knight(0, 1, Config.NumberOfKnights - 1, rostrum, drinkingBout);
            knights[Config.NumberOfKnights - 1] =
                new Knight(Config.NumberOfKnights - 1, 0, Config.NumberOfKnights - 2, rostrum, drinkingBout);

            return knights;
        }

        private static (Thread[], Thread[]) StartParty(Knight[] knights, DrinkingBout drinkingBout)
        {
            var kThreads = StartKnightsTittleTattle(knights);
            var wThreads = ServeCucumbersAndNonAlcoholicWine(drinkingBout);

            return (kThreads, wThreads);
        }

        private static void StopParty(Thread[] kThreads, Thread[] wThreads)
        {
            foreach (var thread in kThreads)
            {
                thread.Join();
            }

            foreach (var thread in wThreads)
            {
                thread.Join();
            }

            Console.WriteLine("Party finished. Every one has fallen asleep.");
        }

        private static Thread[] StartKnightsTittleTattle(Knight[] knights)
        {
            var kThreads = new Thread[knights.Length];

            for (int i = 0; i < knights.Length; i++)
            {
                int _i = i;
                kThreads[i] = new Thread(() =>
                {
                    knights[_i].Revel();
                });

                kThreads[i].Start();
            }

            return kThreads;
        }

        private static Thread[] ServeCucumbersAndNonAlcoholicWine(DrinkingBout drinkingBout)
        {
            var waiterOne = new Waiter(drinkingBout, WaiterType.NonAlcoholicWineWaiter);
            var waiterTwo = new Waiter(drinkingBout, WaiterType.CucumberWaiter);

            var waiterOneThread = new Thread(() =>
            {
                waiterOne.Serve();
            });

            var waiterTwoThread = new Thread(() =>
            {
                waiterTwo.Serve();
            });

            waiterOneThread.Start();
            waiterTwoThread.Start();

            return new Thread[]
            {
                waiterOneThread,
                waiterTwoThread
            };
        }
    }
}
