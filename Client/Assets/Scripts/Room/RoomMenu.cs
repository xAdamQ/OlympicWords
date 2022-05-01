// using Cysharp.Threading.Tasks;
// using UnityEngine;
// using UnityEngine.AddressableAssets;
//
// public class RoomMenu : MonoBehaviour
// {
//     // [Inject] private readonly IRoomController _roomController;
//     // [Inject] private readonly LobbyController.Factory _lobbyFactory;
//     //
//     // [Inject] private readonly IController _controller;
//     // [Inject] private readonly BlockingOperationManager _blockingOperationManager;
//
//     public async static UniTask<RoomMenu> Create()
//     {
//         return (await Addressables.InstantiateAsync("roomMenu"));
//     }
//
//     public async void Surrender()
//     {
//         await BlockingOperationManager.I.Start(Controller.I.Surrender());
//
//         //-----this is not called because finalize panel is shown
//         // _roomController.DestroyModuleGroup();
//         // _lobbyFactory.Create();
//     }
// }

