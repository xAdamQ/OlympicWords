using System.Threading.Tasks;
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

        return new OfflineRepo(c, new Mock<IScopeRepo>().Object, GetLogger<IOfflineRepo>());
    }

    [Fact]
    public async Task Test1()
    {
        var repo = GetOfflineRepo();
        var u = await repo.GetUserByIdAsyc("0");
        var bot999 = await repo.GetUserByIdAsyc("999");
        var u3 = await repo.GetUserByIdAsyc("9999");

        var aa = u.FollowingRelations;
        var bb = u.FollowerRelations;
        var aa2 = bot999.FollowingRelations;
        var bb2 = bot999.FollowerRelations;
        var aa3 = u3.FollowingRelations;
        var bb3 = u3.FollowerRelations;

        u.Followings.Remove(bot999);
        repo.SaveChangesAsync();
    }
}