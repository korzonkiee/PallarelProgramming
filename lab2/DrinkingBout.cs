using System;

namespace monitors
{
    public class DrinkingBout
    {
        // Crockery set represents plates and cups.
        // Cups are those elements indexed with even numbers
        // and plates are those indexed with odd numbers.
        private byte[] crockerySet = new byte[Config.NumberOfKnights];

        // Array of bools indicating whether certain cup or plate
        // is being currencly used by knight.
        private bool[] crockerySetUsage = new bool[Config.NumberOfKnights];

        // Current status of bottle.
        // Maximum size limited by Config.BottleCapcity.
        private byte bottle;

        // Array of condition variables - each for every knight w/ corresponding index.
        // If knight is not able to eat/drink at a certain point in time - he waits.
        // He will be signalized by DrinkingBout about posibility to eat/drink. 
        private ConditionVariable[] knightCanEatCVs = new ConditionVariable[Config.NumberOfKnights];
        private bool[] waitingKnights = new bool[Config.NumberOfKnights];

        // Monitor's lock - only one thread at a time can make use of the monitor.
        private readonly object lockObj = new object();

        public DrinkingBout()
        {
            for (int i = 0; i < knightCanEatCVs.Length; i++)
            {
                knightCanEatCVs[i] = new ConditionVariable();
            }
        }

        // Method used by waiter to fill bottle.
        // Each time the bottle is filled we check if we can
        // satify some thirsty/hungry knights.
        public void FillBottle()
        {
            lock (lockObj)
            {
                Console.WriteLine("Waiter fills bottle.");

                bottle = Config.BottleCapacity;

                TrySatisfySomeKnights();
            }
        }

        // Method used by waiter to fill cucumber plates.
        // Each time plates are filled with cucumbers we check
        // if we can satify some thirsty/hungry knights.
        public void FillCucumberPlates()
        {
            lock (lockObj)
            {
                Console.WriteLine("Waiter fills plates with cucumbers.");

                for (int i = 1; i < crockerySet.Length; i += 2)
                {
                    crockerySet[i] = Config.PlateCapacity;
                }

                TrySatisfySomeKnights();
            }
        }

        private void TrySatisfySomeKnights()
        {
            for (int i = 0; i < waitingKnights.Length; i++)
            {
                if (WantsEat(i) && CanEat(i))
                {
                    WakeUpKnightIfNecessary(i);

                    // In order to avoid unneccessary wakeups
                    // skip one neighbour - he will not be able
                    // to eat anyway.
                    i++;
                }
            }
        }

        // Method used by Knight
        public void StartDrinking(Knight knight)
        {
            lock (lockObj)
            {
                Console.WriteLine($"{knight.ToString()}. Attempts drinking.");

                int i = knight.Idx;

                while (!CanEat(i))
                {
                    Console.WriteLine($"{knight.ToString()}. Can't eat. Waiting.");

                    waitingKnights[i] = true;
                    knightCanEatCVs[i].Wait(lockObj);
                    waitingKnights[i] = false;

                    Console.WriteLine($"{knight.ToString()}. Signalized.");
                }

                Console.WriteLine($"{knight.ToString()}. Drinking.");
                PourWineToCup(i);
                EatCucumber(i);
            }
        }

        public void StopDrinking(Knight knight)
        {
            lock (lockObj)
            {
                Console.WriteLine($"{knight.ToString()}. Stops drinking.");

                ReleaseDrinkingAccessories(knight.Idx);

                WakeUpKnightIfNecessary(knight.L_Idx);
                WakeUpKnightIfNecessary(knight.R_Idx);
            }
        }

        // When Knight stops drinking he checks if his neighbour wants to drink
        // and if he is even able to drink. If he is then simply wake them up.
        private void WakeUpKnightIfNecessary(int i)
        {
            if (WantsEat(i) && CanEat(i))
            {
                Console.WriteLine($"Signal[K{i}]");
                knightCanEatCVs[i].Pulse();
            }
        }

        private void EatCucumber(int i)
        {
            int plateIndex = GetPlateForKnight(i);
            crockerySet[plateIndex]--;
            crockerySetUsage[plateIndex] = true;
        }

        private void PourWineToCup(int i)
        {
            // We don't need to worry about bottle getting < 0.
            // PoutWineToCup is called only after CanEat is true.
            // CanEat method ensures that bottle is not empty.
            bottle -= 1;

            int cupIndex = GetCupForKnight(i);
            crockerySet[cupIndex] = 1;
            crockerySetUsage[cupIndex] = true;
        }

        private int GetCupForKnight(int i)
        {
            // If knight has odd index
            // then cup is on his left.
            if (i % 2 == 1)
                return i - 1;

            // And otherwise - if knight has even index
            // then cup is on his right side.
            return i;
        }

        private int GetPlateForKnight(int i)
        {
            // If knight is King (zero index)
            // then his plate is at index (NumberOfKnights - 1).
            if (i == 0)
                return Config.NumberOfKnights - 1;

            // If knight has odd index
            // then plate is on his right (next index).
            if (i % 2 == 1)
                return i;

            // And otherwise - if knight has even index
            // then plate is on his left side.
            return i - 1;
        }

        private bool CanEat(int i)
        {
            if (BottleIsEmpty())
            {
                return false;
            }

            int plate = GetPlateForKnight(i);
            int cup = GetCupForKnight(i);

            if (PlateIsBeingUsed(plate) || CupIsBeingUsed(cup))
            {
                return false;
            }

            if (PlateHasCucumbers(plate))
            {
                return true;
            }

            return false;
        }

        private bool WantsEat(int i)
        {
            bool wantsEat = waitingKnights[i];

            if (wantsEat)
                Console.WriteLine($"Knight {i} wants eat.");

            return wantsEat;
        }

        private bool BottleIsEmpty()
        {
            bool isEmpty = bottle == 0;

            if (isEmpty)
                Console.WriteLine("Bottle is empty.");

            return isEmpty;
        }

        private bool CupHasWine(int cup)
        {
            bool hasWine = crockerySet[cup] > 0;

            if (!hasWine)
                Console.WriteLine($"No wine in cup {cup}");

            return hasWine;
        }

        private bool PlateHasCucumbers(int plate)
        {
            bool hasCucumbers = crockerySet[plate] > 0;

            if (!hasCucumbers)
                Console.WriteLine($"No cumbers on plate {plate}");

            return hasCucumbers;
        }

        private bool CupIsBeingUsed(int cup)
        {
            bool isUsed = crockerySetUsage[cup];

            if (isUsed)
                Console.WriteLine($"Plate {cup} is being used.");

            return isUsed;
        }

        private bool PlateIsBeingUsed(int plate)
        {
            bool isUsed = crockerySetUsage[plate];

            if (isUsed)
                Console.WriteLine($"Plate {plate} is being used.");

            return isUsed;
        }

        private void ReleaseDrinkingAccessories(int i)
        {
            int plate = GetPlateForKnight(i);
            int cup = GetCupForKnight(i);

            crockerySetUsage[plate] = false;
            crockerySetUsage[cup] = false;
        }
    }
}