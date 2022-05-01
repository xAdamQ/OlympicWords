using Microsoft.AspNetCore.SignalR;
using OlympicWords.Services;

namespace OlympicWords;

public interface IScopeInfo
{
    RoomUser RoomUser { get; }
    void Init(string userId);
}

public class ScopeInfo : IScopeInfo
{
    private IOnlineRepo OnlineRepo { get; }
    private IHubContext<MasterHub> HubContext { get; }
    private string UserId { get; set; }

    public RoomUser RoomUser
    {
        get
        {
            if (roomUser != null)
                return roomUser;

            roomUser = OnlineRepo.GetRoomUserWithId(UserId);
            return roomUser;
        }
    }

    private RoomUser roomUser;

    public ScopeInfo(IOnlineRepo onlineRepo, IHubContext<MasterHub> hubContext)
    {
        this.OnlineRepo = onlineRepo;
        this.HubContext = hubContext;
    }

    public void Init(string userId)
    {
        this.UserId = userId;
    }
}