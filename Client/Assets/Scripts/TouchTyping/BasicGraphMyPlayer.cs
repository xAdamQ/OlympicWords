using DG.Tweening;
using UnityEngine;

public class BasicGraphMyPlayer : MyPlayerBase
{
    [SerializeField] private Material wordHighlightMat, digitHighlightMat, fadeMaterial;
    protected override void Start()
    {
        base.Start();

        MovedADigit += OnMyDigitMoved;
        MovedAWord += ColorWord;

        ColorWord(0);
        ColorChar(0);

        BasicGraphEnv.I.WordState(0, true);
        BasicGraphEnv.I.WordState(1, true);
    }

    private void OnMyDigitMoved()
    {
        ColorChar(0);

        MinimizeChar(TextPointer);
    }


    private void ColorChar(int charIndex)
    {
        Env.GetCharObjectAt(charIndex)
            .GetComponent<Renderer>().material = wordHighlightMat;

        // if (charIndex == EnvBase.I.Text.Length - 1)
        //     return;

        // var isLastLetter = charIndex == Env.GetWordLengthAt(wordIndex) - 1;
        // var isLastWord = wordIndex == EnvBase.I.WordsCount - 1;
        // if (isLastLetter && isLastWord)
        //     return;
        // var targetWord = isLastLetter ? wordIndex + 1 : wordIndex;
        // var targetLetter = isLastLetter ? 0 : charIndex + 1;
        //
        // Env.GetWordObjects(targetWord)[targetLetter]
        //     .GetComponent<Renderer>().material = digitHighlightMat;
    }

    private void ColorWord(int wordIndex)
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

        var lastWordIndex = WordIndex;

        base.JetJump(count);

        BasicGraphEnv.I.WordState(WordIndex, true);
        if (WordIndex + 1 < EnvBase.I.WordsCount)
            BasicGraphEnv.I.WordState(WordIndex + 1, true);

        for (var i = lastWordIndex + 1; i <= WordIndex - 1; i++)
            BasicGraphEnv.I.WordState(i, false);
    }

    private void MinimizeChar(int charIndex)
    {
        var digit = Env.GetCharObjectAt(charIndex);

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

        base.JumpWord(); //word index got updated here

        //current word + 1
        if (WordIndex < EnvBase.I.WordsCount - 1)
            BasicGraphEnv.I.WordState(WordIndex + 1, true);
    }
}