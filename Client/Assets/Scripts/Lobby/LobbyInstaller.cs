// using Cysharp.Threading.Tasks;
// using UnityEngine;
// using UnityEngine.AddressableAssets;
// using UnityEngine.EventSystems;
//
// public class LobbyInstaller : MonoInstaller
// {
//     [SerializeField] private GameObject publicMinUserViewPrefab;
//     [SerializeField] private GameObject friendsViewPrefab;
//     [SerializeField] private GameObject roomChoiceViewPrefab;
//     [SerializeField] private GameObject personalActiveUserViewPrefab;
//
//     [SerializeField] private GameObject CameraPrefab, EventSystemPrefab;
//     [SerializeField] private GameObject standardCanvasPrefab;
//
//     [SerializeField] private GameObject cardbackShopPrefab;
//     [SerializeField] private GameObject cardbackShopItemPrefab;
//     [SerializeField] private AssetReference cardbackSheetRef;
//
//     [InjectOptional] private Settings _settings = new Settings();
//
//     public class Settings
//     {
//         public bool EnableLobbyController;
//         public bool EnableMinUserViewFactory;
//         public bool EnableFirendsView;
//         public bool EnablePersonalActiveUserView;
//         public bool EnablePersonalFullUserView;
//         public bool EnableCardbackShop;
//         public bool EnableRoomChoicesView;
//
//         public Settings(bool defaultServiceState = true)
//         {
//             EnableLobbyController = defaultServiceState;
//             EnableMinUserViewFactory = defaultServiceState;
//             EnableFirendsView = defaultServiceState;
//             EnablePersonalActiveUserView = defaultServiceState;
//             EnablePersonalFullUserView = defaultServiceState;
//             EnableCardbackShop = defaultServiceState;
//             EnableRoomChoicesView = defaultServiceState;
//         }
//     }
//
//     public override void InstallBindings()
//     {
//         if (!FindObjectOfType<Camera>()) ProjectContext.Instance.Container.InstantiatePrefab(CameraPrefab);
//         if (!FindObjectOfType<EventSystem>()) ProjectContext.Instance.Container.InstantiatePrefab(EventSystemPrefab);
//         //general across scenes
//
//         var standardCanvas = Container.InstantiatePrefab(standardCanvasPrefab).transform;
//
//         if (_settings.EnableLobbyController)
//             Container.BindInterfacesAndSelfTo<LobbyController>().AsSingle().NonLazy();
//
//         if (_settings.EnableMinUserViewFactory)
//             Container.BindFactory<MinUserView, MinUserView.BasicFactory>()
//                 .FromComponentInNewPrefab(publicMinUserViewPrefab);
//
//         if (_settings.EnableFirendsView)
//             Container.AddInstantSceneModule<FriendsView>(friendsViewPrefab, standardCanvas);
//
//         if (_settings.EnablePersonalActiveUserView)
//             Container.AddInstantSceneModule<PersonalActiveUserView>(personalActiveUserViewPrefab, standardCanvas);
//
//         if (_settings.EnableCardbackShop)
//         {
//             var res = Shop.Create(standardCanvas, ItemType.Cardback);
//             var res2 = Shop.Create(standardCanvas, ItemType.Background);
//             // while (res.Status != UniTaskStatus.Succeeded && res2.Status != UniTaskStatus.Succeeded)
//             // {
//             // }
//         }
//
//         // Container.AddInstantSceneModule<Shop>(cardbackShopPrefab, standardCanvas)
//         // .WithArguments(cardbackSheetRef, cardbackShopItemPrefab);
//
//         if (_settings.EnableRoomChoicesView)
//             Container.InstantiatePrefab(roomChoiceViewPrefab, standardCanvas);
//         // Container.AddInstantSceneModule<RoomRequester>(roomChoiceViewPrefab, standardCanvas);
//     }
// }

