// using System.Collections.Generic;
// using System.Linq;
// using DG.Tweening;
// using UnityEngine;
//
// public abstract class StairBasedGenerator : MonoModule<StairBasedGenerator>
// {
//     [SerializeField] private GameObject endPlanPrefab, stairPrefab, digitPrefab;
//
//     [SerializeField] public Vector2 spacing;
//     [SerializeField] private float sameDegreeSpacing = 4f;
//
//     public List<List<Stair>> Stairs = new();
//     public List<Stair> MyStairs => Stairs.First();
//
//     public bool useConnected;
//
//     public void GenerateStairs(List<string> words)
//     {
//         words.Reverse();
//
//         var d0 = CalcDiameter(words[0].Length);
//
//         for (var i = 1; i < words.Count; i++)
//         {
//             var dn = CalcDiameter(words[i].Length);
//             if (dn - i * spacing.x > d0) //multiply i with the actual spacing
//                 d0 = dn - i * spacing.x;
//         }
//
//         for (var i = 0; i < words.Count; i++)
//         {
//             GenerateStairsLevel(d0, i, words[i]);
//         }
//
//         foreach (var playerStairs in Stairs)
//         {
//             playerStairs.Reverse();
//         }
//     }
//
//     private float prevDegreeEndAngle = 0f;
//
//     private void GenerateStairsLevel(float baseDiameter, int degree, string word)
//     {
//         var stairSize = word.Length;
//         var portionAngle = 2 * Mathf.PI / RoomController.I.Capacity; //we divide 360 degree over players count
//         var absoluteAngle = prevDegreeEndAngle;
//         var fullStairSize = stairSize + sameDegreeSpacing;
//         for (var i = 0; i < RoomController.I.Capacity; i++)
//         {
//             var stair = Instantiate(stairPrefab).GetComponent<Stair>();
//
//             var fullDiameter = baseDiameter + degree * spacing.x;
//
//             var rotationAngle = i == 0 && useConnected ? +Mathf.Atan(fullStairSize / 2f / fullDiameter) : portionAngle;
//             absoluteAngle += rotationAngle;
//
//             if (i == 0 && useConnected)
//                 prevDegreeEndAngle = absoluteAngle + Mathf.Atan(fullStairSize / 2f / fullDiameter);
//
//
//             //poz
//             var dir = new Vector3(Mathf.Sin(absoluteAngle), 0, Mathf.Cos(absoluteAngle));
//             var poz = dir * fullDiameter;
//             poz.y = degree * spacing.y;
//             stair.transform.position = poz;
//
//             //angle
//             stair.transform.LookAt(Vector3.zero);
//             var stairAngle = stair.transform.eulerAngles;
//             stairAngle.x = 0;
//             stair.transform.eulerAngles = stairAngle;
//
//             //material
//             stair.GetComponent<Renderer>().sharedMaterial = PlayerMats[i];
//
//             //scale
//             var scale = stair.transform.localScale;
//             scale.x = stairSize;
//             stair.transform.localScale = scale;
//
//             //digits
//             for (var d = 0; d < stairSize; d++)
//             {
//                 var digitPoz = Vector3.right * (-.5f + (d + .5f) / stairSize);
//                 var digit = Instantiate(digitPrefab, stair.transform);
//                 //the y scale of the stair is not related to digit y because digit is rotated
//                 digit.transform.localScale = new Vector3(1 / stair.transform.localScale.x, 1, 1);
//                 digit.transform.localPosition = digitPoz;
//                 digit.GetComponent<TextMesh>().text = word[d].ToString();
//             }
//
//             stair.Word = word;
//
//             Stairs[i].Add(stair);
//         }
//     }
//
//     private float CalcDiameter(int stairSize)
//     {
//         var angle = 2 * Mathf.PI / RoomController.I.Capacity; //we divide 360 degree over players count
//         var totalStairSize = stairSize + sameDegreeSpacing;
//
//         return totalStairSize * .5f / Mathf.Tan(angle * .5f);
//     }
//
//
//     public Material[] PlayerMats;
//
//     private void MakePlayersColorPalette()
//     {
//         var baseMat = new Material(stairPrefab.GetComponent<Renderer>().sharedMaterial);
//
//         PlayerMats = new Material[RoomController.I.Capacity];
//         for (var i = 0; i < RoomController.I.Capacity; i++)
//         {
//             PlayerMats[i] = new Material(baseMat)
//             {
//                 color = distinctColors[i] //new Color(Random.Range(0f,1f),Random.Range(0f,1f), Random.Range(0f,1f))
//             };
//         }
//     }
//
//     private Color[] distinctColors = new Color[]
//     {
//         Color.red,
//         Color.blue,
//         Color.yellow,
//         Color.green,
//         Color.white,
//         Color.black,
//         Color.magenta,
//         Color.cyan,
//         Color.Lerp(Color.red, Color.blue, .5f),
//         Color.Lerp(Color.green, Color.blue, .5f),
//         Color.Lerp(Color.green, Color.red, .5f),
//     };
//
//     private void Start()
//     {
//         for (var i = 0; i < RoomController.I.Capacity; i++)
//             Stairs.Add(new List<Stair>());
//         MakePlayersColorPalette();
//     }
// }

