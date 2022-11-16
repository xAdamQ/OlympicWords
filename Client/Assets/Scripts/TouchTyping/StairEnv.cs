using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class StairEnv : EnvBase
{
    public new static StairEnv I;

    [SerializeField] private GameObject stairPrefab, stepPrefab;
    public List<List<Stair>> stairs = new();
    private List<Stair> myStairs => stairs[0];

    public bool moveSteps;
    public float fadeStepValue, fadeStepTime;
    [SerializeField] private int myStepsLayer = 6;
    public Vector3 spacing;


    // protected override void GenerateDigits(string word, Stair stair)
    // {
    //     //digits
    //     for (var d = 0; d < word.Length; d++)
    //     {
    //         var digitPoz = useRepetition
    //             ? Vector3.right * d + Vector3.up * .1f
    //             : Vector3.right * (-.5f + (d + .5f) / word.Length) + Vector3.up * 6f;
    //
    //         var digit = Instantiate(digitPrefab, stair.transform);
    //         //the y scale of the stair is not related to digit y because digit is rotated
    //         digit.transform.localScale =
    //             new Vector3(1 / stair.transform.localScale.x, 1 / stair.transform.localScale.z, 1);
    //         digit.transform.localPosition = digitPoz;
    //         digit.GetComponent<TextMesh>().text = word[d].ToString();
    //     }
    //
    //     stair.Word = word;
    // }


    public override GameObject[] GetWordObjects(int wordIndex)
    {
        throw new NotImplementedException();
    }

    public override int WordsCount => throw new NotImplementedException();

    protected override void Awake()
    {
        base.Awake();
        I = this;
    }

    /// <summary>
    /// when envebase calls his method, it will call this, because this is right version (not tested)
    /// </summary>
    public override void PrepareRequestedRoomRpc(List<FullUserInfo> userInfos, int myTurn, string text,
        List<(int index, int player)> fillerWords, List<int> chosenPowerUps)
    {
        base.PrepareRequestedRoomRpc(userInfos, myTurn, text, fillerWords, chosenPowerUps);

        for (var i = 0; i < Capacity; i++)
            stairs.Add(new List<Stair>());

        GenerateStairs(Words);

        if (TestController.I.UseTest)
        {
            spacing = TestController.I.spacing;
            moveSteps = TestController.I.moveSteps;
            fadeStepValue = TestController.I.fadeStepValue;
            fadeStepTime = TestController.I.fadeStepTime;
            useConnected = TestController.I.useConnected;
            circular = TestController.I.circular;
            useRepetition = TestController.I.useRepetition;
        }
    }

    protected override void GenerateDigits()
    {
        throw new NotImplementedException();
    }

    // private void GenerateDigits(string word, Stair stair)
    // {
    //     //digits
    //     for (var d = 0; d < word.Length; d++)
    //     {
    //         var digitPoz = useRepetition
    //             ? Vector3.right * d + Vector3.up * .1f
    //             : Vector3.right * (-.5f + (d + .5f) / word.Length) + Vector3.up * 6f;
    //
    //         var digit = Instantiate(digitPrefab, stair.transform);
    //         //the y scale of the stair is not related to digit y because digit is rotated
    //         digit.transform.localScale =
    //             new Vector3(1 / stair.transform.localScale.x, 1 / stair.transform.localScale.z, 1);
    //         digit.transform.localPosition = digitPoz;
    //         digit.GetComponent<TextMesh>().text = word[d].ToString();
    //     }
    //
    //     stair.Word = word;
    // }

    public bool useConnected, circular, useRepetition;

    private void GenerateStairs(List<string> words)
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

    private float CalcDiameter(int stairSize)
    {
        var angle = 2 * Mathf.PI / Capacity; //we divide 360 degree over players count
        var totalStairSize = stairSize + spacing.z;

        return totalStairSize * .5f / Mathf.Tan(angle * .5f);
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

        foreach (var playerStairs in stairs)
            playerStairs.Reverse();
        //reverse each player stairs list because we reversed at the start
    }

    private void GenerateLinearStairs(List<string> words)
    {
        var pozPointer = Vector3.up * 10;
        for (var i = 0; i < words.Count; i++)
        {
            //pozPointer.x += words[i].Length / 2f + spacing.x / 2f;
            for (var j = 0; j < Capacity; j++)
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

                //todo implement GenerateDigits, and get the needed word here
                // GenerateDigits(words[i], stair);

                stairs[j].Add(stair);
            }

            pozPointer.x +=
                words[i].Length / 1f +
                spacing.x / 1f; //stair.GetComponent<Renderer>().bounds.max.x;
            //I made the size always 1, changes the models accordingly 
            pozPointer.y += spacing.y;
            pozPointer.z = 0;
        }
    }

    private float prevDegreeEndAngle;

    private void GenerateStairsLevel(float baseDiameter, int degree, string word)
    {
        var stairSize = word.Length;
        var portionAngle = 2 * Mathf.PI / Capacity; //we divide 360 degree over players count
        var absoluteAngle = prevDegreeEndAngle;
        var fullStairSize = stairSize + spacing.z;
        for (var i = 0; i < Capacity; i++)
        {
            var stair = Instantiate(stairPrefab).GetComponent<Stair>();

            var fullDiameter = baseDiameter + degree * spacing.x;

            var rotationAngle = i == 0 && useConnected
                ? +Mathf.Atan(fullStairSize / 2f / fullDiameter)
                : portionAngle;
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

            //todo implement GenerateDigits, and get the needed word here
            // GenerateDigits(word, stair);

            stairs[i].Add(stair);
        }
    }


    public override Vector3 GetDigitPozAt(int wordIndex, int digitIndex)
    {
        throw new NotImplementedException();
    }

    public override Vector3 GetDigitRotAt(int wordIndex, int digitIndex)
    {
        throw new NotImplementedException();
    }
}