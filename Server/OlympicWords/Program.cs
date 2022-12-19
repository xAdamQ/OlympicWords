using Alachisoft.NCache.EntityFrameworkCore;
using Hangfire;
using Hangfire.MemoryStorage;
using Lib.AspNetCore.ServerSentEvents;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using Microsoft.EntityFrameworkCore;
using OlympicWords.Filters;
using OlympicWords.Services;

var builder = WebApplication.CreateBuilder(args);


//services
var configuration = builder.Configuration;

// var hostAddress = Dns.GetHostAddresses("").First();

// builder.WebHost.UseUrls("http://*:5112");

// builder.WebHost.ConfigureKestrel(serverOptions =>
// {
//     serverOptions.Listen(hostAddress, 5112,
//         listenOptions =>
//         {
//             listenOptions.UseHttps(options =>
//             {
//                 
//             });
//         });
// });

//how to enable the tls?

// builder.Services.AddCors();
var services = builder.Services;


services.AddSignalR(options =>
    {
        options.AddFilter<BadUserInputFilter>();
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    })
    .AddAzureSignalR(opt => { opt.ServerStickyMode = ServerStickyMode.Preferred; })
    .AddJsonProtocol(options => { options.PayloadSerializerOptions.IncludeFields = true; });

services.AddHttpContextAccessor();

services.AddControllers(opt => { opt.Filters.Add<DefaultActionFilter>(); })
    .AddJsonOptions(opt => opt.JsonSerializerOptions.IncludeFields = true);

services.AddServerSentEvents();

services.AddDbContext<MasterContext>(options =>
{
    NCacheConfiguration.Configure("default", DependencyType.SqlServer);
    NCacheConfiguration.ConfigureLogger(logLevel: LogLevel.Information);

    options.UseSqlServer(configuration.GetConnectionString("Main"));
    options.EnableSensitiveDataLogging();
});

services.AddScoped<IGameplay, Gameplay>();
services.AddScoped<IOfflineRepo, OfflineRepo>();
services.AddScoped<IFinalizer, Finalizer>();
services.AddScoped<ILobbyManager, LobbyManager>();
// services.AddScoped<IChatManager, ChatManager>();
services.AddScoped<IMatchMaker, MatchMaker>();
services.AddScoped<IScopeRepo, ScopeRepo>();
services.AddScoped<SecurityManager>();

services.AddSingleton(new PersistantData());
services.AddSingleton<IServerLoop, ServerLoop>();

services.AddAuthentication(MasterAuthHandler.PROVIDER_NAME)
    .AddScheme<MasterAuthSchemeOptions, MasterAuthHandler>(MasterAuthHandler.PROVIDER_NAME, null);

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

app.UseRouting();

app.UseAuthentication();
app.MapControllers();

app.UseCors(corsPolicyBuilder => corsPolicyBuilder
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
);

app.MapGet("/weatherforecast", () => new List<int> { 1, 2, 3 });
app.MapServerSentEvents("/updates");

app.UseHttpsRedirection();

app.UseEndpoints(endpoint => endpoint.MapHub<MasterHub>("/connect"));


app.Run();

public partial class Program
{
}