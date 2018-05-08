using System;

namespace Shared
{
    public class Position
    {
        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }

    public static class PositionExtensions
    {
        public static double DistanceTo(this Position from, Position to)
        {
            int Δx = from.X - to.X;
            int Δy = from.Y - to.Y;

            return Math.Sqrt(Math.Pow(Δx, 2) + Math.Pow(Δy, 2));
        }
    }
}