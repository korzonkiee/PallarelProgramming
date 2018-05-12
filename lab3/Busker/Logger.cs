using System;
using Shared;
using Shared.Messages;

namespace Busker
{
    using static Stateless.StateMachine<State, Trigger>;

    public static class Logger
    {
        public static void Log(Busker busker, Message message)
        {
            Console.WriteLine($"{busker.ToString()} receives: {message.ToString()}");
        }

        public static void LogTransition(Busker busker, Transition trans)
        {
            Console.WriteLine($"{busker.ToString()} changes state from {trans.Source} to {trans.Destination}");
        }
    }
}