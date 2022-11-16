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
        ColorCurrentDigit(WordIndex, CharIndex);

        MinimizeDigit(WordIndex, CharIndex);
    }


    private void ColorCurrentDigit(int wordIndex, int letterIndex)
    {
        Env.GetWordObjects(wordIndex)[letterIndex]
            .GetComponent<Renderer>().material = wordHighlightMat;

        var isLastLetter = letterIndex == Env.GetWordLengthAt(wordIndex) - 1;
        var isLastWord = wordIndex == EnvBase.I.WordsCount - 1;
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

    //jumping visuals are for my player only
    protected override void JetJump(int count)
    {
        BasicGraphEnv.I.WordState(WordIndex, false);
        for (var i = CharIndex; i < EnvBase.I.GetWordLengthAt(WordIndex); i++)
            MinimizeDigit(WordIndex, i);

        var lastWordIndex = WordIndex;

        base.JetJump(count);

        BasicGraphEnv.I.WordState(WordIndex, true);
        if (WordIndex + 1 < EnvBase.I.WordsCount)
            BasicGraphEnv.I.WordState(WordIndex + 1, true);

        for (var i = lastWordIndex + 1; i <= WordIndex - 1; i++)
        {
            BasicGraphEnv.I.WordState(i, false);
            //     for (var j = 0; j < EnvBase.I.GetWordLengthAt(i); j++)
            //         MinimizeDigit(i, j);
        }
    }

    private void MinimizeDigit(int wordIndex, int digitIndex)
    {
        var digit = Env.GetWordObjects(wordIndex)[digitIndex];

        var digitRenderer = digit.GetComponent<Renderer>();

        digitRenderer.material = fadeMaterial;
        digitRenderer.material.DOFade(.3f, .3f)
            .SetEase(Ease.OutCirc)
            .OnComplete(() => fadeMaterial.color = Color.white);

        digit.transform.DOScale(.5f, .3f);
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