// using Microsoft.AspNetCore.SignalR;
// using OlympicWords.Services.Extensions;
//
// namespace OlympicWords.Services;
//
// public interface IChatManager
// {
//     Task ShowMessage(string msgId);
// }
//
// public class ChatManager : IChatManager
// {
//     private readonly IHubContext<MasterHub> masterHub;
//     private readonly IScopeRepo scopeRepo;
//
//     public ChatManager(IHubContext<MasterHub> masterHub, IScopeRepo scopeRepo)
//     {
//         this.masterHub = masterHub;
//         this.scopeRepo = scopeRepo;
//     }
//
//     private static readonly HashSet<string> EmojiIds = new()
//     {
//         "angle",
//         "angry",
//         "dead",
//         "cry",
//         "devil",
//         "heart",
//         "cat1",
//         "cat2",
//         "cat3",
//         "moon",
//         "mindBlow",
//         "bigEye",
//         "frog",
//         "laughCry",
//     };
//
//     private static readonly HashSet<string> TextIds = new()
//     {
//         "soLucky",
//         "comeAgain",
//         "congrates",
//         "tough",
//         "kofta",
//         "anyWords",
//         "kossa",
//     };
//
//
//     public async Task ShowMessage(string msgId)
//     {
//         var roomUser = scopeRepo.RoomActor;
//         
//         if (!EmojiIds.Contains(msgId) && !TextIds.Contains(msgId))
//             throw new Exceptions.BadUserInputException("message Id is not valid");
//
//         var oppos = roomUser.Room.RoomUsers.Where(u => u != roomUser).Select(u => u.ActiveUser);
//
//         foreach (var oppo in oppos)
//             await masterHub.SendOrderedAsync(oppo, "ShowMessage", roomUser.TurnId, msgId);
//     }
// }

