namespace Shared
{
    public static class IdGenerator
    {
        private static int counter = 0;

        public static int GetId()
        {
            counter++;
            return counter;
        }
    }
}