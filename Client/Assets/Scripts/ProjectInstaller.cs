// using UnityEngine;
// using UnityEngine.EventSystems;
//
// public class ProjectInstaller : MonoInstaller
// {
//     [SerializeField] private GameObject
//         lobbyContextPrefab,
//         roomContextPrefab,
//         finalizeContextPrefab,
//         blockingPanelPrefab,
//         standardCanvasPrefab,
//         cameraPrefab,
//         eventSystemPrefab,
//         toastsPrefab,
//         fullUserViewPrefab,
//         globalBackgroundPrefab;
//
//     public class Settings
//     {
//         public bool EnableController;
//         public bool EnableRepository;
//         public bool EnableBlockingPanel;
//         public bool EnableBlockingOperationManager;
//         public bool EnableToast;
//         public bool EnableReferenceInstantiator;
//         public bool EnableFullUserView;
//         public bool EnableGlobalBackground;
//
//         public bool EnableLobbyFactory;
//         public bool EnableRoomFactory;
//         public bool EnableFinalizeFactory;
//
//         public Settings(bool defaultServiceState)
//         {
//             EnableReferenceInstantiator = defaultServiceState;
//             EnableController = defaultServiceState;
//             EnableRepository = defaultServiceState;
//             EnableLobbyFactory = defaultServiceState;
//             EnableRoomFactory = defaultServiceState;
//             EnableBlockingPanel = defaultServiceState;
//             EnableBlockingOperationManager = defaultServiceState;
//             EnableToast = defaultServiceState;
//             EnableFullUserView = defaultServiceState;
//             EnableGlobalBackground = defaultServiceState;
//             EnableFinalizeFactory = defaultServiceState;
//         }
//     }
//
//     [InjectOptional] private Settings _moduleSwitches = new Settings(true);
//
//     [System.Serializable]
//     public class References
//     {
//         //this class contains scenes assigning and dyanamic assigning like this
//         [HideInInspector] public Transform Canvas;
//     }
//
//     [SerializeField] private References references;
//
//
//     public override void InstallBindings()
//     {
//         var loadedCanvas = FindObjectOfType<Canvas>();
//         references.Canvas = (!loadedCanvas) ? Instantiate(standardCanvasPrefab).transform : loadedCanvas.transform;
//
//         Container.BindInstance(references);
//
//         Container.BindInstance(standardCanvasPrefab.GetComponent<Canvas>());
//
//         if (!FindObjectOfType<Camera>()) Container.InstantiatePrefab(cameraPrefab);
//         if (!FindObjectOfType<EventSystem>()) Container.InstantiatePrefab(eventSystemPrefab);
//
//
//         if (_moduleSwitches.EnableReferenceInstantiator)
//             Container.Bind<ReferenceInstantiator<ProjectInstaller>>().AsSingle();
//
//         if (_moduleSwitches.EnableController)
//             Container.BindInterfacesTo<Controller>().AsSingle();
//
//         if (_moduleSwitches.EnableRepository)
//             Container.BindInterfacesTo<Repository>().AsSingle();
//
//         if (_moduleSwitches.EnableLobbyFactory)
//             Container.BindFactory<LobbyController, LobbyController.Factory>()
//                 .FromSubContainerResolve()
//                 .ByNewContextPrefab(lobbyContextPrefab);
//
//         if (_moduleSwitches.EnableRoomFactory)
//             Container.BindFactory<RoomSettings, ActiveRoomState, RoomController, RoomController.Factory>()
//                 .FromSubContainerResolve()
//                 .ByNewContextPrefab<RoomInstaller>(roomContextPrefab);
//
//         if (_moduleSwitches.EnableBlockingPanel)
//             Container.AddInstantSceneModule<BlockingPanel>(blockingPanelPrefab, references.Canvas, hasAbstraction: true);
//
//         if (_moduleSwitches.EnableBlockingOperationManager)
//             Container.Bind<BlockingOperationManager>().AsSingle();
//
//         if (_moduleSwitches.EnableFullUserView)
//             Container.AddInstantSceneModule<FullUserView>(fullUserViewPrefab, references.Canvas);
//
//         Background.Create().Forget();
//
//         // if (_moduleSwitches.EnableGlobalBackground)
//         // Container.AddInstantSceneModule<Background>(globalBackgroundPrefab, references.Canvas);
//
//
//         Container.AddInstantSceneModule<Toast>(toastsPrefab, references.Canvas, hasAbstraction: true);
//     }
// }

