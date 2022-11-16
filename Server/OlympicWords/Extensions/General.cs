using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Serialization;

namespace OlympicWords.Services.Extensions
{
    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        private readonly SnakeCaseNamingStrategy _newtonsoftSnakeCaseNamingStrategy = new();

        public static SnakeCaseNamingPolicy Instance { get; } = new();

        public override string ConvertName(string name)
        {
            /* A conversion to snake case implementation goes here. */

            return _newtonsoftSnakeCaseNamingStrategy.GetPropertyName(name, false);
        }
    }

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

        public static void Append<T>(this ConcurrentDictionary<int, T> concurrentDictionary,
            ref int lastId, T value)
        {
            concurrentDictionary.TryAdd(Interlocked.Increment(ref lastId), value);
        }

        public static async Task<object> InvokeAsync(this MethodInfo mi, object obj,
            params object[] parameters)
        {
            dynamic awaitable = mi.Invoke(obj, parameters);

            await awaitable;

            return awaitable.GetAwaiter().GetResult();
        }

        public static async Task InvokeActionAsync(this MethodInfo mi, object obj,
            params object[] parameters)
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
            ActiveUser activeUser, string method, object arg1, object arg2, object arg3)
            where T : Hub
        {
            await hub.Clients.User(activeUser.Id).SendCoreAsync(method,
                new[] { activeUser.MessageIndex++, arg1, arg2, arg3 });
        }

        public static async Task SendOrderedAsync<T>(this IHubContext<T> hub,
            ActiveUser activeUser, string method, object arg1, object arg2, object arg3,
            object arg4) where T : Hub
        {
            await hub.Clients.User(activeUser.Id).SendCoreAsync(method,
                new[] { activeUser.MessageIndex++, arg1, arg2, arg3, arg4 });
        }

        public static async Task SendOrderedAsync<T>(this IHubContext<T> hub,
            ActiveUser activeUser, string method, object arg1, object arg2, object arg3,
            object arg4, object arg5)
            where T : Hub
        {
            await hub.Clients.User(activeUser.Id).SendCoreAsync(method,
                new[] { activeUser.MessageIndex++, arg1, arg2, arg3, arg4, arg5 });
        }

        public static T GetLoggedInUserId<T>(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            var loggedInUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (typeof(T) == typeof(string))
                return (T)Convert.ChangeType(loggedInUserId, typeof(T));

            if (typeof(T) == typeof(int) || typeof(T) == typeof(long))
            {
                return loggedInUserId != null
                    ? (T)Convert.ChangeType(loggedInUserId, typeof(T))
                    : (T)Convert.ChangeType(0, typeof(T));
            }

            throw new Exception("Invalid type provided");
        }

        public static T GetRandom<T>(this IList<T> list)
        {
            var randomIndex = StaticRandom.GetRandom(list.Count);
            return list[randomIndex];
        }
    }
}