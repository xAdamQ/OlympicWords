using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OlympicWords.Services;
using Xunit;
using Xunit.Abstractions;

namespace Tests;

public class UnitTest1
{
    private readonly ITestOutputHelper testOutputHelper;

    private ILogger<T> GetLogger<T>() => XUnitLogger.CreateLogger<T>(testOutputHelper);

    private string ConnectionString =
        "data source=.; Initial Catalog=OlympicWords; persist security info=True; Integrated Security=SSPI;";

    public UnitTest1(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    private IOfflineRepo GetOfflineRepo()
    {
        var options = new DbContextOptionsBuilder<MasterContext>()
            // .UseInMemoryDatabase(databaseName: "testDb")
            .UseSqlServer(ConnectionString)
            .LogTo(testOutputHelper.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging()
            .Options;

        var c = new MasterContext(options);

        return new OfflineRepo(c, new Mock<IScopeRepo>().Object, GetLogger<OfflineRepo>());
    }

    [Fact]
    public async Task Test1()
    {
        // var repo = GetOfflineRepo();
        // var u = await repo.GetUserByIdAsync("0");
        // var bot999 = await repo.GetUserByIdAsync("999");
        // var u3 = await repo.GetUserByIdAsync("9999");
        //
        // var aa = u.FollowingRelations;
        // var bb = u.FollowerRelations;
        // var aa2 = bot999.FollowingRelations;
        // var bb2 = bot999.FollowerRelations;
        // var aa3 = u3.FollowingRelations;
        // var bb3 = u3.FollowerRelations;
        //
        // u.Followings.Remove(bot999);
        // await repo.SaveChangesAsync();
    }

    [Fact]
    public async Task TestCreateFillers()
    {
        var room = new Room(0, 0);
        room.Text =
            "i do well in school and people think i am smart because of it but its not true in fact three years ago i struggled in school however two years ago i decided to get serious about school and made a few changes";

        room.Words = room.Text.Split(" ").ToList();
        room.RoomActors.Add(new RoomUser("00", room, "123")
        {
            ChosenPowerUp = (int)PowerUp.Filler,
        });
        room.RoomActors.Add(new RoomUser("11", room, "32321")
        {
            ChosenPowerUp = (int)PowerUp.Filler,
        });
        room.RoomActors.Add(new RoomUser("11", room, "23445")
        {
            ChosenPowerUp = (int)PowerUp.Filler,
        });

        var sr = new Mock<IScopeRepo>();
        sr.SetupGet(r => r.Room).Returns(room);

        var or = new Mock<IOfflineRepo>();
        or.SetupGet(r => r.SmallFillers).Returns(new[]
        {
            "actually", "just", "really", "totally", "simply", "somehow",
        });
        or.SetupGet(r => r.MediumFillers).Returns(new[]
        {
            "kind of", "by the way", "basically"
        });
        or.SetupGet(r => r.LargeFillers).Returns(new[]
        {
            "at the end of the day", "you know what I mean", "you know, like I said",
            "something like that",
        });


        var mm = new MatchMaker(new Mock<IHubContext<MasterHub>>().Object, or.Object,
            sr.Object, new Mock<IGameplay>().Object, new Mock<IServerLoop>().Object,
            new Mock<ILogger<MatchMaker>>().Object);

        mm.SetFillers(room);
    }

    [Fact]
    public async Task TestProcessChar()
    {
        var room = new Room(0, 0);
        room.Text = "i do well in school and people think i am smart";

        // "i do well in school and people think i am smart because of it but its not true in fact three years ago i struggled in school however two years ago i decided to get serious about school and made a few changes";

        room.Words = room.Text.Split(" ").ToList();
        room.RoomActors.Add(new RoomUser("00", room, null)
        {
            ChosenPowerUp = (int)PowerUp.Filler,
        });
        room.RoomActors.Add(new RoomUser("11", room, null)
        {
            ChosenPowerUp = (int)PowerUp.Filler,
        });
        room.RoomActors.Add(new RoomUser("11", room, null)
        {
            ChosenPowerUp = (int)PowerUp.Filler,
        });

        var sr = new Mock<IScopeRepo>();
        sr.SetupGet(r => r.Room).Returns(room);

        var or = new Mock<IOfflineRepo>();
        or.SetupGet(r => r.SmallFillers).Returns(new[]
        {
            "actually", "just", "really", "totally", "simply", "somehow",
        });
        or.SetupGet(r => r.MediumFillers).Returns(new[]
        {
            "kind of", "by the way", "basically"
        });
        or.SetupGet(r => r.LargeFillers).Returns(new[]
        {
            "at the end of the day", "you know what I mean", "you know, like I said",
            "something like that",
        });

        sr.SetupGet(r => r.RoomActor).Returns(room.RoomActors.First);

        var f = new Mock<IFinalizer>();
        f.Setup(x => x.FinalizeUser()).Callback(() => testOutputHelper.WriteLine("finalize"));

        var gp = new Gameplay(new Mock<IHubContext<MasterHub>>().Object, new Mock<ILogger<Gameplay>>().Object,
            f.Object, new Mock<IServerLoop>().Object, sr.Object);

        var mm = new MatchMaker(new Mock<IHubContext<MasterHub>>().Object, or.Object,
            sr.Object, new Mock<IGameplay>().Object, new Mock<IServerLoop>().Object,
            new Mock<ILogger<MatchMaker>>().Object);

        mm.SetFillers(room);

        foreach (var chr in room.Text)
            await gp.ProcessChar(chr);

        f.Verify(mock => mock.FinalizeUser(), Times.Once());
    }
}