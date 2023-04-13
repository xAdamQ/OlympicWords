using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(GraphJumpPlayer))]
//you can insert additional base classes when needed
public class GraphJumpController : GraphController<GraphJumpPlayer>
{
    public JumpControllerConfig JumpConfig;

    protected override Transform CameraTarget => Player.currentLetter.transform;
    protected override void Awake()
    {
        base.Awake();

        JumpConfig = Player.JumpControllerConfig;
    }

    protected override void Start()
    {
        base.Start();


        AssignEvents();

        ColorWord(0);
        ColorChar(0);
    }

    private void AssignEvents()
    {
        Player.LetterDone += _ =>
        {
            ColorChar(0);
            MinimizeChar(Player.TextPointer);
        };

        Player.MovedAWord += wordIndex =>
        {
            foreach (var digit in GraphEnv.I.GetWordObjects(wordIndex, Player.Index))
            {
                digit.GetComponent<Renderer>().material = WordHighlightMat;
                digit.layer = 7;
            }
        };
    }


    private void ColorChar(int charIndex)
    {
        GraphEnv.I.GetCharObjectAt(charIndex, Player.Index)
            .GetComponent<Renderer>().material = WordHighlightMat;
    }

    private void ColorWord(int wordIndex)
    {
        foreach (var digit in GraphEnv.I.GetWordObjects(wordIndex, Player.Index))
        {
            digit.GetComponent<Renderer>().material = WordHighlightMat;
            digit.layer = 7;
        }
    }

    private void MinimizeChar(int charIndex)
    {
        var digit = GraphEnv.I.GetCharObjectAt(charIndex, Player.Index);

        var digitRenderer = digit.GetComponent<Renderer>();

        digitRenderer.material = FadeMaterial;
        digitRenderer.material.DOFade(.3f, .3f)
            .SetEase(Ease.OutCirc)
            .OnComplete(() => FadeMaterial.color = Color.white);

        digit.transform.DOScale(.5f, .3f);
    }
}