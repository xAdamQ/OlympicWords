using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OlympicWords.Services.Helpers
{
    public class SnakePropertyContractResolver : DefaultContractResolver
    {
        public SnakePropertyContractResolver()
        {
            NamingStrategy = new SnakeCaseNamingStrategy();
        }
    }
    public static partial class Helper
    {
        public static JsonSerializerSettings SnakePropertyNaming { get; } = new JsonSerializerSettings() { ContractResolver = new SnakePropertyContractResolver() };
    }
}