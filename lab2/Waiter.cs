using System.Threading.Tasks;

namespace monitors
{
    public class Waiter
    {
        private readonly DrinkingBout drinkingBout;
        private readonly WaiterType type;

        private delegate void Service();

        public Waiter(DrinkingBout drinkingBout, WaiterType type)
        {
            this.drinkingBout = drinkingBout;
            this.type = type;
        }

        public void Serve()
        {
            Service service = null;

            switch (type)
            {
                case WaiterType.CucumberWaiter:
                    service = new Service(FillCucumberPlates);
                    break;
                case WaiterType.NonAlcoholicWineWaiter:
                    service = new Service(FillBottle);
                    break;
            }

            while (!Config.PartyOver)
            {
                service.Invoke();
                Task.Delay(5000).Wait();
            }
        }

        private void FillCucumberPlates()
        {
            drinkingBout.FillCucumberPlates();

        }

        private void FillBottle()
        {
            drinkingBout.FillBottle();
        }
    }
}