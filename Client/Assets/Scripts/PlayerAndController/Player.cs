using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(PlayerMapper))]
public abstract class Player : MonoBehaviour
{
    [HideInInspector] public float StartTime;

    [HideInInspector] public int
        Index,
        WordIndex,
        TextPointer;

    public ControllerConfig ControllerConfig;

    [HideInInspector] public PlayerMapper Mapper;
    public PlayerConfig Config;

    private char CurrentChar => RootEnv.I.Text[TextPointer];

    [HideInInspector] public PowerUp ChosenPowerUp;
    private int usedJets;

    private List<int> fillerWords;

    protected float OriginalJumpTime, JumpTime, AutomationSpeedUp, JetJumpSlowDown;

    protected virtual void Awake()
    {
        Mapper = GetComponent<PlayerMapper>();
        animator = GetComponent<Animator>();

        //I copy them so I don't edit the original values
        OriginalJumpTime = JumpTime = Config.JumpTime;
        AutomationSpeedUp = Config.AutomationSpeedUp;
        JetJumpSlowDown = Config.JetJumpSlowDown;
    }

    protected virtual void Start()
    {
        RootEnv.I.GameStarted += OnGameStarted;
    }

    public void Init(int index, int powerUp, List<int> myFillers, string name)
    {
        Index = index;
        ChosenPowerUp = (PowerUp)powerUp;
        Mapper.nameText.text = name;
        fillerWords = myFillers;
    }

    private void OnGameStarted()
    {
        StartTime = Time.time;
    }

    public bool IsFinished()
    {
        return TextPointer >= RootEnv.I.Text.Length;
    }

    public event Action MovedADigit;
    public event Action<int> MovedAWord;

    public void TakeInput(char chr)
    {
        //supposed we won't have \r naturally in the text
        if (chr is '\r' or '\n')
        {
            if (ChosenPowerUp == PowerUp.MegaJet && usedJets < 1)
                JetJump(4);
            else if (ChosenPowerUp == PowerUp.SmallJet && usedJets < 2)
                JetJump(1);
        }
        else if (chr == CurrentChar)
        {
            CharJump();
        }
    }

    private void CharJump()
    {
        PrepareNewJump();
        JumpToCurrent();
        //CharIndex is the coming character, 

        MovedADigit?.Invoke();
        TextPointer++;

        //don't jump to the next word if there are no more words
        if (IsFinished())
            return;

        // if (CharIndex == EnvBase.I.GetWordLengthAt(WordIndex))
        if (RootEnv.I.Text[TextPointer - 1] == ' ')
            JumpWord();
    }

    private void PrepareNewJump()
    {
        lastMoveTween.SkipTween();
        lastRotateTween.SkipTween();
        stepMoveTween.SkipTween();
    }

    public (Vector3 start, Vector3 end) MovePath { get; set; }
    protected abstract Tween JumpMovement();
    protected abstract Tween JumpRotation();

    public event Action Jumping, JumpOrdered, JumpFinished;
    private void JumpToCurrent()
    {
        Jumping?.Invoke();
        JumpMovement();

        lastMoveTween = JumpMovement();
        lastRotateTween = JumpRotation();

        lastMoveTween.OnComplete(() => JumpFinished?.Invoke());

        JumpOrdered?.Invoke();
    }

    private Tween lastMoveTween;
    private Tween lastRotateTween;
    private Tween stepMoveTween;

    public Action WordJumping, WordJumped;
    private void JumpWord()
    {
        WordJumping?.Invoke();
        WordIndex++;

        //we have fillers, and the coming is at least a filler
        if (fillerWords is { Count: > 0 } && WordIndex == fillerWords[0])
            StartCoroutine(SkipWord());
        else
            MovedAWord?.Invoke(WordIndex);

        WordJumped?.Invoke();
    }

    public Action WordSkipping, WordSkipped;
    private IEnumerator SkipWord()
    {
        WordSkipping?.Invoke();

        fillerWords.RemoveAt(0);

        var original = JumpTime;
        JumpTime /= AutomationSpeedUp;

        while (CurrentChar != ' ')
        {
            CharJump();
            yield return new WaitForSeconds(JumpTime);
        }

        JumpTime = original;
        WordSkipped?.Invoke();

        CharJump();
        //this can make recursive call to skip work, I put it at the end to make it sequential
    }

    public event Action JetJumping;
    public event Action<int> JetJumped;

    private void JetJump(int count)
    {
        JetJumping?.Invoke();
        var lastWordIndex = WordIndex;

        Mapper.jetpack.SetActive(true);

        JumpTime *= JetJumpSlowDown;

        PrepareNewJump();

        var consumedWords = 0;

        if (CurrentChar == ' ') TextPointer++;
        //in case we are in the start of a word

        for (; consumedWords < count && !IsFinished(); TextPointer++)
        {
            if (CurrentChar != ' ') continue;
            consumedWords++;
            WordIndex++;
        }

        if (IsFinished())
        {
            WordIndex = RootEnv.I.WordsCount - 1;

            jumpPreChar();

            //finish directly because time is critical, the player will see
            //the animation through finalize panel anyway
            RootEnv.I.FinishGame();
        }
        else
        {
            jumpPreChar();
        }

        void jumpPreChar()
        {
            TextPointer--;
            JumpToCurrent();
            TextPointer++;
        }

        usedJets++;

        JetJumped?.Invoke(lastWordIndex);

        HideJetpack();
    }

    private void HideJetpack()
    {
        UniTask.Create(async () =>
        {
            JumpTime = OriginalJumpTime;
            await UniTask.Delay(TimeSpan.FromSeconds(Config.JetpackTime));
            Mapper.jetpack.SetActive(false);
        });
    }

    public static readonly string[] Titles =
    {
        "Basra Player",
        "piece of skill",
        "holy son",
        "basra grandmaster",
        "top eater",
    };
    protected Animator animator;
}