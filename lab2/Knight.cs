using System;
using System.Threading.Tasks;

namespace monitors
{
    public class Knight
    {
        public int Idx { get; }
        public int R_Idx { get; set; }
        public int L_Idx { get; set; }

        public KnightStatus Status { get; set; } = KnightStatus.NotTalking;

        private readonly Rostrum rostrum;
        private readonly DrinkingBout drinkingBout;

        public Knight(int idx, int r_idx, int l_idx,
            Rostrum rostrum, DrinkingBout drinkingBout)
        {
            Idx = idx;
            R_Idx = r_idx;
            L_Idx = l_idx;

            this.rostrum = rostrum;
            this.drinkingBout = drinkingBout;
        }

        public void Revel()
        {
            int drinkingCounter = 0;
            while (true)
            {
                if (drinkingCounter >= 10)
                    break;

                KnightActivity activity = ChooseRandomActivity();

                switch (activity)
                {
                    case KnightActivity.Talking:
                        PerformSpeech();
                        break;
                    case KnightActivity.Drinking:
                        PerformNoAlcoholization();
                        drinkingCounter++;
                        break;
                    case KnightActivity.Sleeping:
                        Sleep();
                        break;
                }
            }

            Config.FallenKnights++;
            if (Config.FallenKnights == Config.NumberOfKnights)
                Config.PartyOver = true;

            Console.WriteLine($"{this.ToString()}. Falling asleep forever.");
        }

        private void PerformSpeech()
        {
            rostrum.StartTalking(this);
            Task.Delay(1000).Wait();
            rostrum.StopTalking(this);
        }

        private void PerformNoAlcoholization()
        {
            drinkingBout.StartDrinking(this);
            Task.Delay(1000).Wait();
            drinkingBout.StopDrinking(this);
        }

        private void Sleep()
        {
            Task.Delay(1000).Wait();
        }

        private KnightActivity ChooseRandomActivity()
        {
            Array values = Enum.GetValues(typeof(KnightActivity));
            Random random = new Random();

            return (KnightActivity)values.GetValue(random.Next(values.Length));
        }

        public override string ToString()
        {
            return $"[K{Idx}]";
        }
    }
}