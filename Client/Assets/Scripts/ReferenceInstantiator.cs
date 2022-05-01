// using UnityEngine;
// using UnityEngine.AddressableAssets;
// using UnityEngine.ResourceManagement.AsyncOperations;
//
// public class ReferenceInstantiator<T> where T : class
// {
//     [Inject] private DiContainer _diContainer;
//
//     public void Instantiate(AssetReference assetReference, System.Action<GameObject> onComplete, Transform parent = null)
//     {
//         assetReference.InstantiateAsync(parent).Completed += handle => OnInstantiateComplete(handle, onComplete);
//     }
//
//     private void OnInstantiateComplete(AsyncOperationHandle<GameObject> handle, System.Action<GameObject> onComplete)
//     {
//         _diContainer.InjectGameObject(handle.Result);
//         onComplete(handle.Result);
//     }
// }

