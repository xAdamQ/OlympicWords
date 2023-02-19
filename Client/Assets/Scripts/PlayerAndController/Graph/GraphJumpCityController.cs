using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(GraphPlayer))]
//you can insert additional base classes when needed
public class GraphJumpCityController : PlayerController<GraphPlayer>
{
    private Vector3 addedOffset;

    private Vector3 lazyYPosition;
    protected override Vector3 TargetLookAt => lazyYPosition;

    protected override void Start()
    {
        base.Start();

        lazyYPosition = transform.position;

        Player.MovedADigit += OnMyDigitMoved;
        Player.MovedAWord += ColorWord;

        Player.JetJumping += OnJetJumping;
        Player.JetJumped += OnJetJumped;

        Player.WordJumping += WordJumping;
        Player.WordJumped += WordJumped;

        Player.Jumped += OnPlayerJumped;
        Player.JumpFinished += OnJumpFinished;

        ColorWord(0);
        ColorChar(0);

        GraphEnv.I.WordState(0, true);
        GraphEnv.I.WordState(1, true);
    }

    protected override Vector3 GetTargetPoz()
    {
        var offset = CameraOffset + addedOffset;

        return
            transform.right * offset.z +
            transform.up * offset.y +
            transform.forward * offset.x +
            TargetLookAt;
    }

    private void OnPlayerJumped()
    {
        addedOffset = Vector3.Distance(Player.MovePath.start, Player.MovePath.end)
                      * JumpZoomCoefficient * Vector3.one;

        StartCoroutine(SetLazyY());
    }

    private IEnumerator SetLazyY()
    {
        var framesCount = MOVE_TIME / Time.fixedDeltaTime;
        var part = 1 / framesCount;
        for (var i = 0; i < framesCount; i++)
        {
            var lazyY = Mathf.Lerp(Player.MovePath.start.y, Player.MovePath.end.y, i * part);
            lazyYPosition = new Vector3(transform.position.x, lazyY, transform.position.z);
            //the look at in the y makes the camera less shaky
            yield return new WaitForFixedUpdate();
        }
    }

    private void OnJumpFinished()
    {
        UniTask.Delay(300).ContinueWith(() => addedOffset = Vector3.zero).Forget();
    }

    private void OnMyDigitMoved()
    {
        ColorChar(0);
        MinimizeChar(Player.TextPointer);
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

    //jumping visuals are for my player only
    private void OnJetJumping()
    {
        GraphEnv.I.WordState(Player.WordIndex, false);
    }

    private void OnJetJumped(int lastWordIndex)
    {
        GraphEnv.I.WordState(Player.WordIndex, true);
        if (Player.WordIndex + 1 < GraphEnv.I.WordsCount)
            GraphEnv.I.WordState(Player.WordIndex + 1, true);

        for (var i = lastWordIndex + 1; i <= Player.WordIndex - 1; i++)
            GraphEnv.I.WordState(i, false);
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

    private void WordJumping()
    {
        //previous word
        GraphEnv.I.WordState(Player.WordIndex, false);
    }
    private void WordJumped()
    {
        //current word + 1
        if (Player.WordIndex < GraphEnv.I.WordsCount - 1)
            GraphEnv.I.WordState(Player.WordIndex + 1, true);
    }
}