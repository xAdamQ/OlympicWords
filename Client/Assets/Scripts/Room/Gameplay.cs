using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Basra.Common;
using BestHTTP.SignalRCore;
using UnityEngine;
using UnityEngine.AddressableAssets;


public class Gameplay : MonoModule<Gameplay>
{
    [SerializeField] private GameObject myPlayerPrefab, oppoPlayerPrefab;
    public bool moveSteps;
    public float fadeStepValue, fadeStepTime;

    public List<string> tstWords;

    public int PlayerCount = 10;

    [SerializeField] private int stepsLayer = 6;

    private HubConnection hubConnection;

    [SerializeField] private GameObject endPlanPrefab, stairPrefab, digitPrefab, stepPrefab;

    [SerializeField] public Vector3 spacing;

    public List<List<Stair>> Stairs = new();
    public List<Stair> MyStairs => Stairs.First();

    public bool useConnected;
    public bool circular;
    public bool useRepetition;

    public void GenerateStairs(List<string> words)
    {
        if (circular)
        {
            GenerateCircularStairs(words);
        }
        else //linear
        {
            GenerateLinearStairs(words);
        }
    }

    private void GenerateCircularStairs(List<string> words)
    {
        words.Reverse();

        var d0 = CalcDiameter(words[0].Length);
        for (var i = 1; i < words.Count; i++)
        {
            var dn = CalcDiameter(words[i].Length);
            if (dn - i * spacing.x > d0) //multiply i with the actual spacing
                d0 = dn - i * spacing.x;
        } //calcing the inner diameter (d0)

        for (var i = 0; i < words.Count; i++)
        {
            GenerateStairsLevel(d0, i, words[i]);
        } //generate each circular level

        foreach (var playerStairs in Stairs)
            playerStairs.Reverse();
        //reverse each player stairs list because we reversed at the start
    }

    private void GenerateLinearStairs(List<string> words)
    {
        var pozPointer = Vector3.zero;
        for (var i = 0; i < words.Count; i++)
        {
            //pozPointer.x += words[i].Length / 2f + spacing.x / 2f;
            for (var j = 0; j < Gameplay.I.PlayerCount; j++)
            {
                var stair = Instantiate(stairPrefab).GetComponent<Stair>();

                if (!useRepetition)
                {
                    var scale = stair.transform.localScale;
                    scale.x = words[i].Length;
                    stair.transform.localScale = scale;
                }
                else
                {
                    var stepPointer = Vector3.left * words[i].Length * 0f;
                    for (var k = 0; k < words[i].Length; k++)
                    {
                        Instantiate(stepPrefab, stepPointer, Quaternion.identity, stair.transform);

                        stepPointer.x++;
                    }
                }

                stair.transform.position = pozPointer;

                pozPointer.z += stair.GetComponent<Renderer>().bounds.size.z + spacing.z;

                //material
                stair.GetComponent<Renderer>().sharedMaterial = PlayerMats[j];

                GenerateDigits(words[i], stair);

                Stairs[j].Add(stair);
            }

            pozPointer.x += words[i].Length / 1f + spacing.x / 1f; //stair.GetComponent<Renderer>().bounds.max.x;
            //I made the size always 1, changes the models accordingly 
            pozPointer.y += spacing.y;
            pozPointer.z = 0;
        }
    }

    private float prevDegreeEndAngle = 0f;

    private void GenerateStairsLevel(float baseDiameter, int degree, string word)
    {
        var stairSize = word.Length;
        var portionAngle = 2 * Mathf.PI / Gameplay.I.PlayerCount; //we divide 360 degree over players count
        var absoluteAngle = prevDegreeEndAngle;
        var fullStairSize = stairSize + spacing.z;
        for (var i = 0; i < Gameplay.I.PlayerCount; i++)
        {
            var stair = Instantiate(stairPrefab).GetComponent<Stair>();

            var fullDiameter = baseDiameter + degree * spacing.x;

            var rotationAngle = i == 0 && useConnected ? +Mathf.Atan(fullStairSize / 2f / fullDiameter) : portionAngle;
            absoluteAngle += rotationAngle;

            if (i == 0 && useConnected)
                prevDegreeEndAngle = absoluteAngle + Mathf.Atan(fullStairSize / 2f / fullDiameter);

            //poz
            var dir = new Vector3(Mathf.Sin(absoluteAngle), 0, Mathf.Cos(absoluteAngle));
            var poz = dir * fullDiameter;
            poz.y = degree * spacing.y;
            stair.transform.position = poz;

            //angle
            stair.transform.LookAt(Vector3.zero);
            var stairAngle = stair.transform.eulerAngles;
            stairAngle.x = 0;
            stair.transform.eulerAngles = stairAngle;

            //material
            stair.GetComponent<Renderer>().sharedMaterial = PlayerMats[i];

            //scale
            var scale = stair.transform.localScale;
            scale.x = stairSize;
            stair.transform.localScale = scale;

            GenerateDigits(word, stair);

            Stairs[i].Add(stair);
        }
    }

    private void GenerateDigits(string word, Stair stair)
    {
        //digits
        for (var d = 0; d < word.Length; d++)
        {
            var digitPoz = useRepetition
                ? Vector3.right * d + Vector3.up * .1f
                : Vector3.right * (-.5f + (d + .5f) / word.Length) + Vector3.up * .1f;

            var digit = Instantiate(digitPrefab, stair.transform);
            //the y scale of the stair is not related to digit y because digit is rotated
            digit.transform.localScale =
                new Vector3(1 / stair.transform.localScale.x, 1 / stair.transform.localScale.z, 1);
            digit.transform.localPosition = digitPoz;
            digit.GetComponent<TextMesh>().text = word[d].ToString();
        }

        stair.Word = word;
    }

    private float CalcDiameter(int stairSize)
    {
        var angle = 2 * Mathf.PI / Gameplay.I.PlayerCount; //we divide 360 degree over players count
        var totalStairSize = stairSize + spacing.z;

        return totalStairSize * .5f / Mathf.Tan(angle * .5f);
    }


    public Material[] PlayerMats;

    private void MakePlayersColorPalette()
    {
        var baseMat = new Material(stairPrefab.GetComponent<Renderer>().sharedMaterial);

        PlayerMats = new Material[Gameplay.I.PlayerCount];
        for (var i = 0; i < Gameplay.I.PlayerCount; i++)
        {
            PlayerMats[i] = new Material(baseMat)
            {
                color = distinctColors[i] //new Color(Random.Range(0f,1f),Random.Range(0f,1f), Random.Range(0f,1f))
            };
        }
    }

    private Color[] distinctColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.yellow,
        Color.green,
        Color.black,
        Color.magenta,
        Color.cyan,
        Color.Lerp(Color.red, Color.blue, .5f),
        Color.Lerp(Color.green, Color.blue, .5f),
        Color.Lerp(Color.green, Color.red, .5f),
    };


    private void Start()
    {
        GenerateStairs(tstWords);

        Stairs[0].ForEach(s =>
        {
            s.gameObject.layer = stepsLayer;
            foreach (Transform digit in s.transform)
                digit.gameObject.layer = stepsLayer;
        });

        for (var i = 0; i < PlayerCount; i++)
            Stairs.Add(new List<Stair>());
        MakePlayersColorPalette();
    }

    public Gameplay()
    {
        I = this;

        Controller.I.AddRpcContainer(this);
    }

    public void CreatePlayers()
    {
        var oppoPlaceCounter = 1;
        //oppo place starts at 1 to 3

        for (int i = 0; i < RoomController.I.Capacity; i++)
        {
            if (RoomController.I.MyTurn == i)
            {
                MyPlayer = Object.Instantiate(myPlayerPrefab).GetComponent<Player>();
                MyPlayer.Init(RoomController.I.MyTurn);

                Players.Add(MyPlayer);

                var cameraFollow = Camera.main!.GetComponent<CameraFollow>();
                cameraFollow.target = MyPlayer.transform;
                cameraFollow.InstantFollow();
            }
            else
            {
                var oppo = Object.Instantiate(oppoPlayerPrefab).GetComponent<Oppo>();
                oppo.Init(oppoPlaceCounter++);

                Players.Add(oppo);
                Oppos.Add(oppo);
            }
        }
    }

    private List<PlayerBase> Players { get; } = new();
    private List<Oppo> Oppos { get; } = new();
    private Player MyPlayer { get; set; }

    public void ResumeGame()
    {
    }

    public void BeginGame()
    {
    }
}