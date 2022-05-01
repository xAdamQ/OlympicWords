using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Tests;

public class Client
{
    public HubConnection HubConnection { get; private set; }

    public int Id { get; }

    private readonly ILogger _logger;

    public Client(int id, ILogger logger)
    {
        Id = id;
        _logger = logger;
    }

    public async Task Connect(HubConnection hubConnection)
    {
        HubConnection = hubConnection;

        SetupRpcs();

        await HubConnection.StartAsync();
    }

    private void SetupRpcs()
    {
    }

    public async Task UpStream()
    {
        await HubConnection.SendAsync("UpStreamChar", clientStreamData());
    }

    async IAsyncEnumerable<char> clientStreamData()
    {
        for (var i = 0; i < 5; i++)
        {
            yield return (char) (i + 10);
            await Task.Delay(10);
        }
    }
}