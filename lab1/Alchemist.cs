using System;
using System.Threading;

namespace lab1
{
    public abstract class Alchemist
    {
        protected string Name { get; private set; }

        public Alchemist()
        {
            Name = this.GetType().Name;
        }

        public abstract void RequestResources(AlchemistsIndulger indulger);
    }

    public class AlchemistA : Alchemist
    {
        public override void RequestResources(AlchemistsIndulger indulger)
        {
            Console.WriteLine($"{Name} requests resources.");
            indulger.ResourceRequestedBy(this);

            indulger.GuildASem.Wait();

            Console.WriteLine($"{Name} has been indulged.");
        }
    }

    public class AlchemistB : Alchemist
    {
        public override void RequestResources(AlchemistsIndulger indulger)
        {
            Console.WriteLine($"{Name} requests resources.");
            indulger.ResourceRequestedBy(this);

            indulger.GuildBSem.Wait();

            Console.WriteLine($"{Name} has been indulged.");
        }
    }

    public class AlchemistC : Alchemist
    {
        public override void RequestResources(AlchemistsIndulger indulger)
        {
            Console.WriteLine($"{Name} requests resources.");
            indulger.ResourceRequestedBy(this);

            indulger.GuildCSem.Wait();

            Console.WriteLine($"{Name} has been indulged.");
        }
    }

    public class AlchemistD : Alchemist
    {
        public override void RequestResources(AlchemistsIndulger indulger)
        {
            Console.WriteLine($"{Name} requests resources.");
            indulger.ResourceRequestedBy(this);

            indulger.GuildDSem.Wait();

            Console.WriteLine($"{Name} has been indulged.");
        }
    }
}