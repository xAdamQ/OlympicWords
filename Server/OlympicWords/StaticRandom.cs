using System;
using System.Threading;

namespace OlympicWords.Services
{
    /// <summary>
    /// random in multithreads
    /// I don't understand the impl, it should do the job
    /// </summary>
    public static class StaticRandom
    {
        private static int seed = Environment.TickCount;

        private static readonly ThreadLocal<Random> Random = new(() => new Random(Interlocked.Increment(ref seed)));

        public static int GetRandom(int min, int max)
        {
            return Random.Value.Next(min, max);
        }
        public static int GetRandom(int max)
        {
            return Random.Value.Next(max);
        }
    }
}