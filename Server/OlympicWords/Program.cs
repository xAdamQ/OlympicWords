using Alachisoft.NCache.EntityFrameworkCore;
using Hangfire;
using Hangfire.MemoryStorage;
using Lib.AspNetCore.ServerSentEvents;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OlympicWords.Filters;
using OlympicWords.Services;

var builder = WebApplication.CreateBuilder(args);

//services
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

services.AddDbContext<MasterContext>(options =>
{
    NCacheConfiguration.Configure("default", DependencyType.SqlServer);
    NCacheConfiguration.ConfigureLogger(logLevel: LogLevel.Information);
    var connection = new SqlConnection(configuration.GetConnectionString("Azure"));
    // var credential = new DefaultAzureCredential();
    // var token = credential.GetToken(new TokenRequestContext(new[] { "https://wordwar.database.windows.net/.default" }));
    // connection.AccessToken = token.Token;
    options.UseSqlServer(connection, opt => { opt.CommandTimeout(30); });
    options.EnableSensitiveDataLogging();
});

services.AddScoped<IGameplay, Gameplay>();
services.AddScoped<IOfflineRepo, OfflineRepo>();
services.AddScoped<IFinalizer, Finalizer>();
services.AddScoped<ILobbyManager, LobbyManager>();
services.AddScoped<IMatchMaker, MatchMaker>();
services.AddScoped<IScopeRepo, ScopeRepo>();
services.AddScoped<SecurityManager>();

services.AddSingleton(new PersistantData());
services.AddSingleton<IServerLoop, ServerLoop>();

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
app.UseEndpoints(endpoint => endpoint.MapHub<RoomHub>("/connect"));

app.MapServerSentEvents("/updates");


app.Run();

public partial class Program
{
}