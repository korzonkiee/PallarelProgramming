using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace lab1
{
    public class AlchemistsIndulger
    {
        private Queue<Alchemist> AlchemistsA = new Queue<Alchemist>();
        private Queue<Alchemist> AlchemistsB = new Queue<Alchemist>();
        private Queue<Alchemist> AlchemistsC = new Queue<Alchemist>();
        private Queue<Alchemist> AlchemistsD = new Queue<Alchemist>();

        public readonly SemaphoreSlim GuildASem = new SemaphoreSlim(0);
        public readonly SemaphoreSlim GuildBSem = new SemaphoreSlim(0);
        public readonly SemaphoreSlim GuildCSem = new SemaphoreSlim(0);
        public readonly SemaphoreSlim GuildDSem = new SemaphoreSlim(0);

        // Binary sempahores that ensures that adding alchemists
        // to the queue is thread-safe.
        private readonly SemaphoreSlim aQueueSem = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim bQueueSem = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim cQueueSem = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim dQueueSem = new SemaphoreSlim(1, 1);

        private readonly SemaphoreSlim resourceCreatedSem;

        private readonly Factory lead;
        private readonly Factory sulfur;
        private readonly Factory mercury;

        public AlchemistsIndulger(SemaphoreSlim resourceCreatedSem,
            Factory lead, Factory sulfur, Factory mercury)
        {
            this.resourceCreatedSem = resourceCreatedSem;

            this.lead = lead;
            this.sulfur = sulfur;
            this.mercury = mercury;
        }

        public void StartIndulging()
        {
            RegisterResourceProducedListener();
        }

        private void RegisterResourceProducedListener()
        {
            Thread thread = new Thread(() =>
            {
                while (true)
                {
                    resourceCreatedSem.Wait();
                    TryDistributeResources();
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        public void ResourceRequestedBy(Alchemist alchemist)
        {
            bool isFirst = false;

            if (alchemist is AlchemistA)
            {
                aQueueSem.Wait();
                isFirst = AlchemistsA.Any();
                AlchemistsA.Enqueue(alchemist);
                aQueueSem.Release();
            }
            else if (alchemist is AlchemistB)
            {
                bQueueSem.Wait();
                isFirst = AlchemistsB.Any();
                AlchemistsB.Enqueue(alchemist);
                bQueueSem.Release();
            }
            else if (alchemist is AlchemistC)
            {
                cQueueSem.Wait();
                isFirst = AlchemistsC.Any();
                AlchemistsC.Enqueue(alchemist);
                cQueueSem.Release();
            }
            else if (alchemist is AlchemistD)
            {
                dQueueSem.Wait();
                isFirst = AlchemistsD.Any();
                AlchemistsD.Enqueue(alchemist);
                dQueueSem.Release();
            }

            if (isFirst)
                TryDistributeResources();
        }

        private void TryDistributeResources()
        {
            Console.WriteLine("[AlchemistsIndulger] Attempting to distribute resources.");

            // Check D
            if (AlchemistsD.Any())
            {
                lead.LockResources();
                sulfur.LockResources();
                mercury.LockResources();

                while (lead.NumberOfResources > 0 && sulfur.NumberOfResources > 0 && mercury.NumberOfResources > 0)
                {
                    lead.AcquireResource();
                    sulfur.AcquireResource();
                    mercury.AcquireResource();

                    dQueueSem.Wait();
                    AlchemistsD.Dequeue();
                    dQueueSem.Release();

                    GuildDSem.Release();
                }

                lead.UnlockResources();
                sulfur.UnlockResources();
                mercury.UnlockResources();
            }

            // Check A
            if (AlchemistsA.Any())
            {
                lead.LockResources();
                mercury.LockResources();

                while (lead.NumberOfResources > 0 && mercury.NumberOfResources > 0)
                {
                    lead.AcquireResource();
                    mercury.AcquireResource();

                    aQueueSem.Wait();
                    AlchemistsA.Dequeue();
                    aQueueSem.Release();

                    GuildASem.Release();
                }

                lead.UnlockResources();
                mercury.UnlockResources();
            }

            // Check B
            if (AlchemistsB.Any())
            {
                sulfur.LockResources();
                mercury.LockResources();

                while (sulfur.NumberOfResources > 0 && mercury.NumberOfResources > 0)
                {
                    sulfur.AcquireResource();
                    mercury.AcquireResource();

                    bQueueSem.Wait();
                    AlchemistsB.Dequeue();
                    bQueueSem.Release();

                    GuildBSem.Release();
                }

                sulfur.UnlockResources();
                mercury.UnlockResources();
            }

            // Check C
            if (AlchemistsC.Any())
            {
                lead.LockResources();
                sulfur.LockResources();

                while (lead.NumberOfResources > 0 && sulfur.NumberOfResources > 0)
                {
                    lead.AcquireResource();
                    sulfur.AcquireResource();

                    cQueueSem.Wait();
                    AlchemistsC.Dequeue();
                    cQueueSem.Release();

                    GuildCSem.Release();
                }

                lead.UnlockResources();
                sulfur.UnlockResources();
            }
        }
    }
}