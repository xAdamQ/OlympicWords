// using UnityEngine;
//
// public static class ZenjectExtensions
// {
//     public static FromBinder AddInstantSceneModule<T>(this DiContainer container, GameObject prefab,
//         Transform parent = null, bool hasAbstraction = false)
//     {
//         var binder = hasAbstraction ? (FromBinder)container.BindInterfacesTo<T>() : (FromBinder)container.Bind<T>();
//
//         if (parent == null)
//             binder.FromComponentInNewPrefab(prefab).AsSingle().NonLazy();
//         else
//             binder.FromComponentInNewPrefab(prefab).UnderTransform(parent).AsSingle().NonLazy();
//
//         return binder;
//     }
// }

