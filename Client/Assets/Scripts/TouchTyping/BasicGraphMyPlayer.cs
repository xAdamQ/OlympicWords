using DG.Tweening;
using UnityEngine;

public class MyCityPlayer : MyPlayerBase
{
    [SerializeField] private Material wordHighlightMat, digitHighlightMat, fadeMaterial;

    protected override void Start()
    {
        base.Start();

        MovedADigit += OnMyDigitMoved;
        MovedAWord += ColorActiveWord;

        ColorActiveWord(0);
        ColorCurrentDigit(0, 0);
    }

    private void OnMyDigitMoved(PlayerBase sender)
    {
        ColorCurrentDigit(sender.WordIndex, sender.DigitIndex);

        var digit = Env.GetWordObjects(sender.WordIndex)[sender.DigitIndex];
        MinimizeDigit(digit);
    }


    private void ColorCurrentDigit(int wordIndex, int letterIndex)
    {
        Env.GetWordObjects(wordIndex)[letterIndex]
            .GetComponent<Renderer>().material = wordHighlightMat;

        var isLastLetter = letterIndex == Env.GetWordLengthAt(wordIndex) - 1;
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
    }
}