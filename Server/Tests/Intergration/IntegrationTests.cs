using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public class IntegrationTests
    {
        private readonly CustomWebApplicationFactory<Program> webApplicationFactory;
        private readonly ITestOutputHelper testOutputHelper;
        private List<Client> Clients { get; } = new();

        public IntegrationTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
            webApplicationFactory = new(this.testOutputHelper);
        }

        private async Task<Client> MakeClient()
        {
            var hubConnection = new HubConnectionBuilder()
                .WithUrl($"http://localhost:5112/connect?access_token={Guid.NewGuid()}",
                    httpConnectionOptions => httpConnectionOptions.HttpMessageHandlerFactory =
                        _ => webApplicationFactory.Server.CreateHandler())
                //I think this creates a fake http message handler in the client, instead of the default real one
                //which is responsible for the http request stuff as content, header, method, etc...
                .Build();

            await Task.Delay(1000);

            var c = new Client(Clients.Count, webApplicationFactory.Logger);

            Clients.Add(c);

            await c.Connect(hubConnection);

            testOutputHelper.WriteLine("a new client is made with index: " + (Clients.Count - 1));

            return c;
        }


        [Fact]
        public async Task InitGame()
        {
            var c = await MakeClient();

            await Task.Delay(1000);
        }

        [Fact(Timeout = 99999999)]
        public async Task SuccessfulRoom()
        {
            var c = await MakeClient();
            var c2 = await MakeClient();

            await Task.Delay(100);

            await c.HubConnection.InvokeAsync("RequestRandomRoom", 0, 0);
            await c2.HubConnection.InvokeAsync("RequestRandomRoom", 0, 0);

            await c.HubConnection.InvokeAsync("Ready");
            await c2.HubConnection.InvokeAsync("Ready");

            await Task.Delay(1000);
        }

        [Fact]
        public async Task UpStreamRandomDigits()
        {
            var c = await MakeClient();

            await c.UpStreamRandomDigits();

            await Task.Delay(1000);
        }


        // [Fact]
        // public async Task TestFirstDistribute()
        // {
        //     var c = await MakeClient();
        //     var c2 = await MakeClient();
        //
        //     await Task.Delay(50);
        //
        //     await c.Connection.InvokeAsync("RequestRandomRoom", 0, 0);
        //     await c2.Connection.InvokeAsync("RequestRandomRoom", 0, 0);
        //
        //     await Task.Delay(50);
        //
        //     await c.Connection.InvokeAsync("Ready");
        //     await c2.Connection.InvokeAsync("Ready");
        //
        //     await Task.Delay(50);
        //
        //     for (int i = 0; i < 4; i++)
        //     {
        //         await c.Connection.InvokeAsync("Throw", 0);
        //         await c2.Connection.InvokeAsync("Throw", 0);
        //     }
        // }
    }
}