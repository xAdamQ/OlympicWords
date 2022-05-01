// using Cysharp.Threading.Tasks;
// using System;
// using UnityEngine;
// using UnityEngine.Networking;
//
// public class MinUserInfoInteractive<TInfo> where TInfo : Basra.Common.MinUserInfo
// {
//     public virtual TInfo Info { get; set; }
//
//     public MinUserInfoInteractive(TInfo info)
//     {
//         Info = info;
//         DownloadPicture().Forget();
//     }
//
//     public Texture2D Picture { get; set; }
//
//     public event Action<Texture2D> PictureLoaded;
//     public bool IsPictureLoaded;
//
//     private async UniTaskVoid DownloadPicture()
//     {
//         await UniTask.DelayFrame(1);
//
//         if (string.IsNullOrEmpty(Info.PictureUrl)) return;
//
//         Picture = await GetRemoteTexture(Info.PictureUrl);
//         PictureLoaded?.Invoke(Picture);
//         IsPictureLoaded = true;
//     }
//
//     public static async UniTask<Texture2D> GetRemoteTexture(string url)
//     {
//         using (var www = UnityWebRequestTexture.GetTexture(url))
//         {
//             // begin request:
//             var asyncOp = www.SendWebRequest();
//
//             // await until it's done: 
//             try
//             {
//                 await asyncOp;
//             }
//             catch (Exception)
//             {
//                 Debug.Log($"{www.error}, URL:{www.url}");
//             }
//
//             // read results:
//             if (www.result == UnityWebRequest.Result.ConnectionError ||
//                 www.result == UnityWebRequest.Result.ProtocolError)
//             {
//                 // log error:
// #if DEBUG
//                 Debug.Log($"{www.error}, URL:{www.url}");
// #endif
//
//                 // nothing to return on error:
//                 return null;
//             }
//
//             // return valid results:
//             Debug.Log($"img from url {url} downloaded");
//             return DownloadHandlerTexture.GetContent(www);
//         }
//     }
// }
//
// public class MinUserInfo : MinUserInfoInteractive<Basra.Common.MinUserInfo>
// {
//     public MinUserInfo(Basra.Common.MinUserInfo info) : base(info)
//     {
//     }
// }