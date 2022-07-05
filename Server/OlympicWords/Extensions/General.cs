using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace OlympicWords.Services.Extensions
{
    public static class General
    {
        public static void Remove<T>(this List<T> list, Predicate<T> predicate)
        {
            list.RemoveAt(list.FindIndex(predicate));
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            var random = new Random();
            for (var i = 0; i < list.Count; i++)
            {
                var temp = list[i];
                var randomIndex = random.Next(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        public static IEnumerable<List<T>> Permutations<T>(this IEnumerable<T> source)
        {
            if (null == source)
                throw new ArgumentNullException(nameof(source));

            var data = source.ToList();

            return Enumerable.Range(0, 1 << (data.Count))
                .Select(index => data.Where((v, i) => (index & (1 << i)) != 0)
                    .ToList());
        }

        public static List<T> CutRange<T>(this List<T> from, int count, bool fromEnd = true)
        {
            var startIndex = fromEnd ? from.Count - count : 0;

            var part = from.GetRange(startIndex, count);
            from.RemoveRange(startIndex, count);

            return part;
        }

        public static T Cut<T>(this List<T> from, bool fromEnd = true)
        {
            var elementIndex = fromEnd ? from.Count - 1 : 0;

            var element = from[elementIndex];
            from.RemoveAt(elementIndex);

            return element;
        }

        public static T Cut<T>(this List<T> from, int index)
        {
            var element = from[index];
            from.RemoveAt(index);

            return element;
        }

        public static bool IsInRange(this int value, int max, int min = 0)
        {
            return value < max && value >= min;
        }

        public static void Append<T>(this ConcurrentDictionary<int, T> concurrentDictionary, ref int lastId, T value)
        {
            concurrentDictionary.TryAdd(Interlocked.Increment(ref lastId), value);
        }

        public static async Task<object> InvokeAsync(this MethodInfo mi, object obj, params object[] parameters)
        {
            dynamic awaitable = mi.Invoke(obj, parameters);

            await awaitable;

            return awaitable.GetAwaiter().GetResult();
        }

        public static async Task InvokeActionAsync(this MethodInfo mi, object obj, params object[] parameters)
        {
            dynamic awaitable = mi.Invoke(obj, parameters);

            await awaitable;
        }

        public static long ToUnixSeconds(this DateTime dateTime)
        {
            return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
        }

        public static double? SecondsPassedSince(this DateTime? dateTime)
        {
            return (DateTime.UtcNow - dateTime)?.TotalSeconds;
        }

        public static async Task SendOrderedAsync<T>(this IHubContext<T> hub,
            ActiveUser activeUser, string method) where T : Hub
        {
            await hub.Clients.User(activeUser.Id).SendCoreAsync(method,
                new[] { (object)activeUser.MessageIndex++ });
        }

        public static async Task SendOrderedAsync<T>(this IHubContext<T> hub,
            ActiveUser activeUser, string method, object arg1) where T : Hub
        {
            await hub.Clients.User(activeUser.Id).SendCoreAsync(method,
                new[] { activeUser.MessageIndex++, arg1 });
        }

        public static async Task SendOrderedAsync<T>(this IHubContext<T> hub,
            ActiveUser activeUser, string method, object arg1, object arg2) where T : Hub
        {
            await hub.Clients.User(activeUser.Id).SendCoreAsync(method,
                new[] { activeUser.MessageIndex++, arg1, arg2 });
        }

        public static async Task SendOrderedAsync<T>(this IHubContext<T> hub,
            ActiveUser activeUser, string method, object arg1, object arg2, object arg3) where T : Hub
        {
            await hub.Clients.User(activeUser.Id).SendCoreAsync(method,
                new[] { activeUser.MessageIndex++, arg1, arg2, arg3 });
        }

        public static async Task SendOrderedAsync<T>(this IHubContext<T> hub,
            ActiveUser activeUser, string method, object arg1, object arg2, object arg3, object arg4) where T : Hub
        {
            await hub.Clients.User(activeUser.Id).SendCoreAsync(method,
                new[] { activeUser.MessageIndex++, arg1, arg2, arg3, arg4 });
        }

        public static async Task SendOrderedAsync<T>(this IHubContext<T> hub,
            ActiveUser activeUser, string method, object arg1, object arg2, object arg3, object arg4, object arg5)
            where T : Hub
        {
            await hub.Clients.User(activeUser.Id).SendCoreAsync(method,
                new[] { activeUser.MessageIndex++, arg1, arg2, arg3, arg4, arg5 });
        }
    }
}