using System.Linq;
using DG.Tweening;
using UnityEngine;

public class BasicGraphMyPlayer : MyPlayerBase
{
    [SerializeField] private Material wordHighlightMat, digitHighlightMat, fadeMaterial;

    protected override void Start()
    {
        base.Start();

        MovedADigit += OnMyDigitMoved;
        MovedAWord += ColorActiveWord;

        ColorActiveWord(0);
        ColorCurrentDigit(0, 0);

        BasicGraphEnv.I.WordState(0, true);
        BasicGraphEnv.I.WordState(1, true);
    }

    private void OnMyDigitMoved()
    {
        ColorCurrentDigit(WordIndex, DigitIndex);

        var digit = Env.GetWordObjects(WordIndex)[DigitIndex];
        MinimizeDigit(digit);
    }


    private void ColorCurrentDigit(int wordIndex, int letterIndex)
    {
        Env.GetWordObjects(wordIndex)[letterIndex]
            .GetComponent<Renderer>().material = wordHighlightMat;

        var isLastLetter = letterIndex == Env.GetWordLengthAt(wordIndex) - 1;
        var isLastWord = wordIndex == RoomBase.I.Words.Length - 1;
        if (isLastLetter && isLastWord) return;
        var targetWord = isLastLetter ? wordIndex + 1 : wordIndex;
        var targetLetter = isLastLetter ? 0 : letterIndex + 1;

        Env.GetWordObjects(targetWord)[targetLetter]
            .GetComponent<Renderer>().material = digitHighlightMat;
    }

    private void ColorActiveWord(int wordIndex)
    {
        foreach (var digit in Env.GetWordObjects(wordIndex))
        {
            digit.GetComponent<Renderer>().material = wordHighlightMat;
            digit.layer = 7;
        }
    }

    private void MinimizeDigit(GameObject digit)
    {
        var digitRenderer = digit.GetComponent<Renderer>();

        digitRenderer.material = fadeMaterial;
        digitRenderer.material.DOFade(.1f, .3f).SetEase(Ease.OutCirc)
            .OnComplete(() => fadeMaterial.color = Color.white);

        digit.transform.DOScale(.3f, .3f);
    }


    protected override void JumpWord()
    {
        //previous word
        BasicGraphEnv.I.WordState(WordIndex, false);

        base.JumpWord();

        //current word + 1
        if (WordIndex + 1 == EnvBase.I.WordsCount) return;

        BasicGraphEnv.I.WordState(WordIndex + 1, true);
    }
}