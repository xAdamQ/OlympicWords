using System.Net;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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
    options.ClientTimeoutInterval = TimeSpan.FromHours(1); //2343
});
services.AddHttpContextAccessor();

services.AddControllers();

services.AddDbContext<MasterContext>(options =>
{
    options.UseSqlServer(configuration.GetConnectionString("Main"));
});

services.AddScoped<IGameplay, Gameplay>();
services.AddScoped<IOfflineRepo, OfflineRepo>();
services.AddScoped<IFinalizer, Finalizer>();
services.AddScoped<ILobbyManager, LobbyManager>();
services.AddScoped<IChatManager, ChatManager>();
services.AddScoped<IMatchMaker, MatchMaker>();
services.AddScoped<IScopeRepo, ScopeRepo>();
services.AddScoped<SecurityManager>();

services.AddSingleton(new PersistantData());
services.AddSingleton(new MasterHub.MethodDomains());
services.AddSingleton<IServerLoop, ServerLoop>();

services.AddAuthentication(MasterAuthenticationHandler.PROVIDER_NAME)
    .AddScheme<MasterAuthenticationSchemeOptions, MasterAuthenticationHandler>(
        MasterAuthenticationHandler.PROVIDER_NAME, null);

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


app.UseAuthentication();
app.MapControllers();

app.UseCors(corsPolicyBuilder => corsPolicyBuilder
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
);

app.MapGet("/weatherforecast", () => new List<int> { 1, 2, 3 });

app.UseRouting();
app.UseEndpoints(endpoint => endpoint.MapHub<MasterHub>("/connect"));


app.Run();

public partial class Program
{
}