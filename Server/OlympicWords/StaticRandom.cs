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
        private static int seed = new Random().Next();
        // private static int seed = Environment.TickCount;

        private static readonly ThreadLocal<Random> random = new(() => new Random(Interlocked.Increment(ref seed)));

        public static int GetRandom(int min, int max)
        {
            return random.Value!.Next(min, max);
        }
        public static int GetRandom(int max)
        {
            return random.Value!.Next(max);
        }
    }
}