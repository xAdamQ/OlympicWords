using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OlympicWords;
using OlympicWords.Services;
using MasterAuthenticationHandler = OlympicWords.MasterAuthenticationHandler;
using MasterAuthenticationSchemeOptions = OlympicWords.MasterAuthenticationSchemeOptions;

var builder = WebApplication.CreateBuilder(args);

//services
var configuration = builder.Configuration;
builder.WebHost.UseUrls("http://*:5112");

// Add services to the container.

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


services.AddAuthentication(MasterAuthenticationHandler.ProviderName)
    .AddScheme<MasterAuthenticationSchemeOptions, MasterAuthenticationHandler>(
        MasterAuthenticationHandler.ProviderName, null);


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

app.UseCors(corsPolicyBuilder => corsPolicyBuilder
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
);
app.UseRouting();

app.UseEndpoints(endpoint => endpoint.MapHub<MasterHub>("/connect"));

app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

app.Run();

public partial class Program
{
}