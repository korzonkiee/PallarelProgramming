using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace monitors
{
    public class Rostrum
    {
        private Knight[] knights;
        private ConditionVariable[] neighsTalkingCVs;
        private ConditionVariable[] kingTalkingCVs;

        private readonly object lockObj = new object();

        public Rostrum()
        {

        }

        public void RegisterKnights(Knight[] knights)
        {
            this.knights = knights;
            this.neighsTalkingCVs = knights
                .Select(_ => new ConditionVariable())
                .ToArray();
            this.kingTalkingCVs = knights
                .Select(_ => new ConditionVariable())
                .ToArray();
        }

        public void StartTalking(Knight knight)
        {
            lock (lockObj)
            {
                Console.WriteLine($"{knight.ToString()}. Attempts talking.");

                int k_idx = knight.Idx;

                while (!IsKing(k_idx) && knight.Status.HasFlag(KnightStatus.ListeningToKing))
                {
                    // Wait until Knight finishes his talking.
                    Console.WriteLine($"{knight.ToString()}. King is talking. Waiting.");
                    knight.Status |= KnightStatus.WaitingForKing;
                    kingTalkingCVs[k_idx].Wait(lockObj);
                    knight.Status &= ~KnightStatus.WaitingForKing;
                }

                Knight l_neigh = knights[knight.L_Idx];
                Knight r_neigh = knights[knight.R_Idx];

                while (l_neigh.Status.HasFlag(KnightStatus.Talking) ||
                        r_neigh.Status.HasFlag(KnightStatus.Talking))
                {
                    Console.WriteLine($"{knight.ToString()}. Neighbour(s) are talking. Waiting.");

                    knight.Status |= KnightStatus.WaitingForNeigh;
                    neighsTalkingCVs[k_idx].Wait(lockObj);
                    knight.Status &= ~KnightStatus.WaitingForNeigh;

                    Console.WriteLine($"{knight.ToString()}. Signalized.");
                }


                // Set talking state.
                knight.Status = KnightStatus.Talking;

                // If kings starts talking he sets status `ListeningToKing`
                // for everyone who is in `NotTalking` status.
                if (IsKing(k_idx))
                {
                    // Omit 0 - it is King himself.
                    for (int i = 1; i < knights.Length; i++)
                    {
                        if (knights[i].Status.HasFlag(KnightStatus.NotTalking))
                        {
                            knights[i].Status |= KnightStatus.ListeningToKing;
                        }
                    }
                }
            }
        }

        public void StopTalking(Knight knight)
        {
            lock (lockObj)
            {
                Console.WriteLine($"{knight.ToString()}. Stops talking.");

                int k_idx = knight.Idx;

                Knight l_neigh = knights[knight.L_Idx];
                Knight r_neigh = knights[knight.R_Idx];

                Knight ll_neigh = knights[l_neigh.L_Idx];
                Knight rr_neigh = knights[r_neigh.R_Idx];

                if (IsKing(k_idx))
                {
                    knight.Status = KnightStatus.NotTalking;

                    // If King has finished his talking
                    // consider every Knight in `ListeningToKing` status.
                    for (int i = 1; i < knights.Length; i++)
                    {
                        if (knights[i].Status.HasFlag(KnightStatus.ListeningToKing))
                        {
                            // If Knight is waiting for someone to end talking
                            // check if it is necessary to wake him up - check his neighbours.
                            if (knights[i].Status.HasFlag(KnightStatus.WaitingForNeigh))
                            {
                                var left = knights[knights[i].L_Idx];
                                var right = knights[knights[i].R_Idx];

                                if (left.Status.HasFlag(KnightStatus.NotTalking) &&
                                    right.Status.HasFlag(KnightStatus.NotTalking))
                                {
                                    neighsTalkingCVs[i].Pulse();
                                }
                            }
                            // If Knight is waiting for King to end talking
                            // then simply wake him up. We don't have to check his neighbours
                            // because after he wakes from waiting for the king he checks
                            // if he can talk (he checks his neighs). If not he goes to sleep.
                            // Hence no need for double check.
                            else if (knights[i].Status.HasFlag(KnightStatus.WaitingForKing))
                            {
                                kingTalkingCVs[i].Pulse();
                            }
                            // For everyone else (who does not want to speak)
                            // Bring back his original state - NotTalking.
                            else
                            {
                                knights[i].Status = KnightStatus.NotTalking;
                            }

                            // knights[i] is no longer listening to king
                            // so remove the `ListeningToKing` flag.
                            knights[i].Status &= ~KnightStatus.ListeningToKing;
                        }
                    }
                }
                else
                {
                    // If Knight (not King) has finished his talking
                    // and in the meantime King is still talking
                    // then set Knight's status to `ListeningToKing`.
                    // `NotTalking` otherwise.
                    if (knights[0].Status.HasFlag(KnightStatus.Talking))
                    {
                        knight.Status = KnightStatus.ListeningToKing;
                    }
                    else
                    {
                        knight.Status = KnightStatus.NotTalking;
                    }

                    // If Knight has finished his talking
                    // and his first neighbours are waiting to tell something
                    // and his second neighbors are not talking (or waiting)
                    // then wake those first neighbours up.
                    if (l_neigh.Status.HasFlag(KnightStatus.WaitingForNeigh) &&
                        ll_neigh.Status.HasFlag(KnightStatus.NotTalking))
                    {
                        Console.WriteLine($"{knight.ToString()}. Signal {l_neigh.ToString()}.");
                        neighsTalkingCVs[l_neigh.Idx].Pulse();
                    }

                    // ...
                    if (r_neigh.Status.HasFlag(KnightStatus.WaitingForNeigh) &&
                        rr_neigh.Status.HasFlag(KnightStatus.NotTalking))
                    {
                        Console.WriteLine($"{knight.ToString()}. Signal {r_neigh.ToString()}.");
                        neighsTalkingCVs[r_neigh.Idx].Pulse();
                    }
                }
            }
        }

        private bool IsKing(int idx)
        {
            return idx == 0;
        }
    }
}