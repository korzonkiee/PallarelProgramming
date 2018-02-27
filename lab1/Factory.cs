using System;
using System.Threading;
using System.Threading.Tasks;

namespace lab1
{
    public sealed class Factory
    {
        public string Name { get; private set; }
        public int NumberOfResources { get; private set; } = Consts.FactoryInitialNumberOfResources;

        private int numberOfCurses = Consts.FactoryInitialNumberOfCurses;

        private const int capacity = Consts.FactoryDefaultCapacity;

        // When variable numberOfProducts becomes lower than capacity
        // then the Factory thread is being singlized by produceSem
        // that it can now start producing resources.
        private readonly SemaphoreSlim produceSem = new SemaphoreSlim(capacity, capacity);

        // When variable numberOfCurses becomes 0
        // then the Factory thread is being signalized by curseSem
        // that it is now able to produce resources.
        private readonly SemaphoreSlim curseSem = new SemaphoreSlim(1, 1);

        // Binary semaphore responsible for synchonized access
        // of variable `numberOfCurses`.
        private readonly SemaphoreSlim numberOfCursesSem = new SemaphoreSlim(1, 1);

        // Binary semaphore responsible for synchonized access
        // of variable `numberOfResources`.
        private readonly SemaphoreSlim numberOfResourcesSem = new SemaphoreSlim(1, 1);

        // Counting sempahore used for signalizing that a resource
        // has been produced.
        private readonly SemaphoreSlim resourceCreatedSem;

        private readonly Random random;
        private bool stopWorking;

        public Factory(string name, SemaphoreSlim resourceCreatedSem)
        {
            this.resourceCreatedSem = resourceCreatedSem;

            Name = name;
            random = new Random();
        }

        public void StartWorking()
        {
            while (!stopWorking)
            {
                ProductResource();
            }
        }

        public void StopWorking()
        {
            stopWorking = true;
        }

        private void ProductResource()
        {
            // Sleep until numberOfProducts becomes 
            // less than capacity.
            produceSem.Wait();

            // Sleep until numberOfCurses becomes 0.
            curseSem.Wait();
            curseSem.Release();

            Console.WriteLine($"Factory {Name} starts to product resource.");

            int resourceTimeMs = random.Next(500, 700);
            Task.Delay(resourceTimeMs).Wait();

            numberOfResourcesSem.Wait();
            NumberOfResources++;
            numberOfResourcesSem.Release();

            Console.WriteLine($"Factory {Name} produced {NumberOfResources} resource.");

            // Signalize that a resource has been created.
            resourceCreatedSem.Release();
        }

        public void AcquireResource()
        {
            // I'm not using semaphore in here because method AcquireResource
            // is used inside thread-safe method AlchemistsIndulger.TryDistributeResources().

            produceSem.Release();
            NumberOfResources--;

            Console.WriteLine($"{Name} resource has been acquired.");
        }

        public void Curse()
        {
            numberOfCursesSem.Wait();

            if (numberOfCurses == 0)
            {
                curseSem.Wait();
                numberOfCurses++;
            }

            numberOfCursesSem.Release();

            Console.WriteLine($"{Name} has been cursed.");
        }

        public void Heal()
        {
            numberOfCursesSem.Wait();

            if (numberOfCurses == 0)
                return;

            numberOfCurses--;
            if (numberOfCurses == 0)
                curseSem.Release();

            numberOfCursesSem.Release();

            Console.WriteLine($"{Name} has been healed.");
        }

        public void LockResources()
        {
            numberOfResourcesSem.Wait();
        }

        public void UnlockResources()
        {
            numberOfResourcesSem.Release();
        }
    }
}