using System.Runtime.InteropServices;
using EFCoreSecondLevelCacheInterceptor;
using Hangfire;
using Hangfire.MemoryStorage;
using Lib.AspNetCore.ServerSentEvents;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OlympicWords;
using OlympicWords.Filters;
using OlympicWords.Services;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

var services = builder.Services;

services.AddSignalR(options =>
    {
        options.AddFilter<BadUserInputFilter>();
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        options.MaximumParallelInvocationsPerClient = 2;
    })
    // .AddAzureSignalR(opt => { opt.ServerStickyMode = ServerStickyMode.Preferred; })
    .AddJsonProtocol(options => { options.PayloadSerializerOptions.IncludeFields = true; });

services.AddHttpContextAccessor();

services.AddControllers(opt => { opt.Filters.Add<DefaultActionFilter>(); })
    .AddJsonOptions(opt => opt.JsonSerializerOptions.IncludeFields = true);

services.AddServerSentEvents();

services.AddHostedService<PendingRoomsLoop>();

#region databse
const string PROVIDER_NAME = "InMemoryDefault";

var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
if (!isWindows && !isLinux)
    throw new Exception("Unsupported OS");

var connectionString = isWindows
    ? configuration.GetConnectionString("Windows")
    : configuration.GetConnectionString("Azure");

services.AddEFSecondLevelCache(opt => opt
    .UseEasyCachingCoreProvider(PROVIDER_NAME, isHybridCache: false)
    .DisableLogging()
    .CacheAllQueries(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(30)));

// More info: https://easycaching.readthedocs.io/en/latest/In-Memory/
services.AddEasyCaching(options => options.UseInMemory(configuration, PROVIDER_NAME));

services.AddDbContext<MasterContext>((serviceProvider, options) =>
{
    var connection = new SqlConnection(connectionString);

    options.UseSqlServer(connection, opt => opt
        .CommandTimeout(30)
        .EnableRetryOnFailure());

    options.EnableSensitiveDataLogging();

    options.AddInterceptors(serviceProvider.GetRequiredService<SecondLevelCacheInterceptor>());
});
#endregion

#region app services
services.AddScoped<IGameplay, Gameplay>();
services.AddScoped<IOfflineRepo, OfflineRepo>();
services.AddScoped<IFinalizer, Finalizer>();
services.AddScoped<ILobbyManager, LobbyManager>();
services.AddScoped<IMatchMaker, MatchMaker>();
services.AddScoped<IScopeRepo, ScopeRepo>();
services.AddScoped<SecurityManager>();

OfflineRepo.Touch();

services.AddSingleton(new PersistantData());
services.AddSingleton<IServerLoop, BotLoop>();
#endregion

services.AddAuthentication(MasterAuthHandler.PROVIDER_NAME)
    .AddScheme<MasterAuthSchemeOptions, MasterAuthHandler>(MasterAuthHandler.PROVIDER_NAME, null);
services.AddAuthorization();

services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseMemoryStorage()
        .WithJobExpirationTimeout(TimeSpan.FromHours(3))
        .UseDefaultTypeSerializer();
});

services.AddHangfireServer();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(corsPolicyBuilder => corsPolicyBuilder
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<RoomHub>("/connect");

app.MapServerSentEvents("/updates");


app.Run();

public partial class Program
{
}