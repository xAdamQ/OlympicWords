using Shared.Controllers;

public static class Controllers
{
    static Controllers()
    {
        User = ControllerProxy<IUserController>.CreateProxy();
        Lobby = ControllerProxy<ILobbyController>.CreateProxy();
    }

    public static readonly IUserController User;
    public static readonly ILobbyController Lobby;
}