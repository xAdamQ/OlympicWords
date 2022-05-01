// using UnityEngine;
// using UnityEngine.AddressableAssets;
// //
// public class FinalizeInstaller : MonoInstaller
// {
//     [SerializeField] private GameObject
//         finalMuvPrefab,
//         finalMuvsParentPrefab;
//
//     [Inject] private readonly Canvas _standardCanvasPrefab;
//
//     [Inject] FinalizeResult _finalizeResult;
//     [Inject] RoomSettings _roomSettings;
//
//     [System.Serializable]
//     public class References
//     {
//         // public AssetReference RoomResultPanelRef;
//
//         //this class contains scenes assigning and dyanamic assigning like this
//         [HideInInspector] public Transform Canvas;
//
//         public static References I;
//     }
//
//     [SerializeField] private References references;
//
//
//     public override void InstallBindings()
//     {
//         references.Canvas = Container.InstantiatePrefab(_standardCanvasPrefab.gameObject).transform;
//
//         var finalMuvsParent = Container.InstantiatePrefab(finalMuvsParentPrefab, references.Canvas).transform;
//
//         // Container.BindInterfacesAndSelfTo<FinalizeController>()
//         //     .AsSingle()
//         //     .WithArguments(_finalizeResult, _roomSettings) //this is not necessary because the face takes it anyway
//         //     .NonLazy();
//
//         //Container.BindFactory<MinUserInfo, UserRoomStatus, FinalMuv, FinalMuv.BasicFactory>()
//         //    .FromComponentInNewPrefab(finalMuvPrefab)
//         //    .UnderTransform(finalMuvsParent);
//
//         // Container.Bind<ReferenceInstantiator<FinalizeInstaller>>().AsSingle();
//
//         // Container.BindInstance(references);
//     }
// }

