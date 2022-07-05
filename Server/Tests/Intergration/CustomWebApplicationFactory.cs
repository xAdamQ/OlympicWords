using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Tests
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup>
        where TStartup : class
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private XUnitLoggerProvider xUnitLoggerProvider;

        public ILogger Logger { get; private set; }

        public CustomWebApplicationFactory(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;

            xUnitLoggerProvider = new XUnitLoggerProvider(_testOutputHelper);
            Logger = xUnitLoggerProvider.CreateLogger("server log");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Register the xUnit logger
            builder.ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder.Services.AddSingleton<ILoggerProvider>(_ => xUnitLoggerProvider);
            });
        }
    }
}