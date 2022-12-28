using System.Reflection;
using Shared.Controllers;
using UnityEngine;

public static class Controllers
{
    static Controllers()
    {
#if !UNITY_WEBGL
        User = ControllerProxy<IUserController>.CreateProxy();
        Lobby = ControllerProxy<ILobbyController>.CreateProxy();
#else
        User = new UserController();
#endif
    }

    public static readonly IUserController User;

    //lobby is not implemented yet, all the functions are gathered from the old game
    public static readonly ILobbyController Lobby;
}