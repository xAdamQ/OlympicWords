// using System;
// using UnityEngine;
// using UnityEngine.UI;
//
// public class ProfilePicture : MonoBehaviour
// {
//     private Image image;
//
//     private void Awake()
//     {
//         image = GetComponent<Image>();
//     }
//
//     public void Init(MinUserInfo minUserInfo)
//     {
//         if (minUserInfo.IsPictureLoaded)
//             SetPicture(minUserInfo.Picture);
//         else
//             minUserInfo.PictureLoaded += pic => SetPicture(pic);
//     }
//
//     private void SetPicture(Texture2D texture2D)
//     {
//         if (texture2D != null)
//             image.sprite =
//                 Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(.5f, .5f));
//     }
// }

