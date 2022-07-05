using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OlympicWords.Common;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

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
        HubConnection.On<PersonalFullUserInfo>(
            "InitGame",
            (p) =>
            {
                _logger.LogInformation($"init game called on {Id} with\n" +
                                       $"personal info is {JsonConvert.SerializeObject(p, Formatting.Indented)}\n");
            });

        HubConnection.On<int, int, List<FullUserInfo>, int>(
            "PrepareRequestedRoomRpc",
            (c, cap, infos, myIndex) =>
            {
                _logger.LogInformation($"init game called on {Id} with\n" +
                                       $"infos is {JsonConvert.SerializeObject(infos, Formatting.Indented)}\n" +
                                       $"my index {myIndex}");
            });
    }

    public async Task UpStreamRandomDigits()
    {
        await HubConnection.SendAsync("UpStreamChar", clientStreamData());
    }

    async IAsyncEnumerable<char> clientStreamData()
    {
        for (var i = 0; i < 5; i++)
        {
            yield return (char)(i + 10);
            await Task.Delay(10);
        }
    }
}