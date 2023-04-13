using System.Reflection;
using Shared.Controllers;
using UnityEngine;

public static class Controllers
{
    static Controllers()
    {
        // User = ControllerProxy<IUserController>.CreateProxy();
        // Lobby = ControllerProxy<ILobbyController>.CreateProxy();

        User = new UserController();
        Lobby = new LobbyController();
    }

    public static readonly IUserController User;

    //lobby is not implemented yet, all the functions are gathered from the old game
    public static readonly ILobbyController Lobby;
}