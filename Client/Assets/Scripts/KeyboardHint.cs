using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class KeyboardHint : MonoModule<KeyboardHint>
{
    private static readonly KeyLetter[][] rows =
    {
        new[]
        {
            new KeyLetter('`', 0),
            new KeyLetter('1', 1),
            new KeyLetter('2', 2),
            new KeyLetter('3', 3),
            new KeyLetter('4', 4),
            new KeyLetter('5', 4),
            new KeyLetter('6', 5),
            new KeyLetter('7', 5),
            new KeyLetter('8', 6),
            new KeyLetter('9', 7),
            new KeyLetter('0', 8),
            new KeyLetter('-', 8),
            new KeyLetter('=', 8),
        },
        new[]
        {
            new KeyLetter('q', 1),
            new KeyLetter('w', 2),
            new KeyLetter('e', 3),
            new KeyLetter('r', 4),
            new KeyLetter('t', 4),
            new KeyLetter('y', 5),
            new KeyLetter('u', 5),
            new KeyLetter('i', 6),
            new KeyLetter('o', 7),
            new KeyLetter('p', 8),
            new KeyLetter('[', 8),
            new KeyLetter(']', 8),
            new KeyLetter('\\', 8),
        },
        new[]
        {
            new KeyLetter('a', 1),
            new KeyLetter('s', 2),
            new KeyLetter('d', 3),
            new KeyLetter('f', 4),
            new KeyLetter('g', 4),
            new KeyLetter('h', 5),
            new KeyLetter('j', 5),
            new KeyLetter('k', 6),
            new KeyLetter('l', 7),
            new KeyLetter(';', 8),
            new KeyLetter('\'', 8),
        },
        new[]
        {
            new KeyLetter('z', 1),
            new KeyLetter('x', 2),
            new KeyLetter('c', 3),
            new KeyLetter('v', 4),
            new KeyLetter('b', 4),
            new KeyLetter('n', 5),
            new KeyLetter('m', 5),
            new KeyLetter(',', 6),
            new KeyLetter('.', 7),
            new KeyLetter('/', 8)
        },
        new[]
        {
            new KeyLetter(' ', 0)
        }
    };

    private static readonly Dictionary<char, KeyLetter> lettersDic =
        rows.SelectMany(k => k).ToDictionary(k => k.letter, k => k);

    [SerializeField] private GameObject letterPrefab, view;
    [SerializeField] private Image[] fingers;
    [SerializeField] private float spacingMultiplier, transparency, hintTime;
    [SerializeField] private RectTransform keyboard;
    [SerializeField] private Color[] groupColors;

    private readonly Dictionary<char, GameObject> letterObjects = new();
    private GameObject leftShiftLetter, rightShiftLetter;
    private Color[] transparentGroupColors;
    private float closeTime;
    private bool shown;


    protected override void Awake()
    {
        base.Awake();


        GenerateKeyboard();
    }

    private void GenerateKeyboard()
    {
        transparentGroupColors = groupColors.Select(c => c - new Color(0f, 0f, 0f, 1f - transparency)).ToArray();
        var fullSize = new Vector2(keyboard.sizeDelta.x / 14.5f, keyboard.sizeDelta.y / rows.Length);
        var size = fullSize - spacingMultiplier * fullSize;
        var currentRow = 0;
        var startX = spacingMultiplier * fullSize.x / 2;
        var pointer = new Vector2(startX, -spacingMultiplier * fullSize.y / 2);

        letterPrefab.GetComponent<RectTransform>().sizeDelta = size;
        letterPrefab.transform.GetChild(0).GetComponent<TMP_Text>().color = new Color(1f, 1f, 1f, transparency);

        DrawRow();
        DrawCustomKey(1.5f, 8);
        NexRow();

        DrawCustomKey(1.5f, 1);
        DrawRow();
        NexRow();

        DrawCustomKey(1.8f, 1);
        DrawRow();
        DrawCustomKey(1.8f, 8);
        NexRow();

        DrawCustomKey(2.35f, 1);
        DrawRow();
        DrawCustomKey(2.35f, 8);
        NexRow();

        pointer.x = Vector2.right.x * (fullSize.x * 4.35f);
        DrawCustomKey(6f, 0, ' ');

        keyboard.GetComponent<Image>().color = new Color(0f, 0f, 0f, transparency);
        view.SetActive(false);

        for (var i = 0; i < fingers.Length - 1; i++)
            fingers[i].color = transparentGroupColors[i];
        fingers.Last().color = transparentGroupColors[0];


        void DrawRow()
        {
            for (var l = 0; l < rows[currentRow].Length; l++)
            {
                var letter = rows[currentRow][l];
                DrawLetter(letter.group, letter.letter);
            }

            currentRow++;
        }
        void DrawCustomKey(float sizeMultiplier, int group, char letter = '#')
        {
            var letterGo = DrawLetter(group);
            var width = size.x * sizeMultiplier;

            var rt = letterGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, size.y);
            pointer += Vector2.right * (sizeMultiplier - 1) * size.x;

            if (letter != '#')
                letterObjects.Add(letter, letterGo);
        }
        void NexRow()
        {
            pointer.x = startX;
            pointer += Vector2.down * fullSize.y;
        }
        GameObject DrawLetter(int group, char letter = '#')
        {
            var l = Instantiate(letterPrefab, keyboard);
            l.GetComponent<RectTransform>().anchoredPosition = pointer;
            l.GetComponent<Image>().color = transparentGroupColors[group];
            pointer += Vector2.right * fullSize.x;

            if (letter != '#')
            {
                letterObjects.Add(letter, l);
                l.transform.GetChild(0).GetComponent<TMP_Text>().text = letter.ToString();
            }

            return l;
        }
    }

    public void ShowHint(char letter)
    {
        closeTime = Time.time + hintTime;

        if (!shown)
            StartCoroutine(ShowHintRoutine(letter));
    }

    private IEnumerator ShowHintRoutine(char letter)
    {
        shown = true;
        view.SetActive(true);

        var k = lettersDic[letter];

        Highlight(letterObjects[letter].GetComponent<Image>());
        Highlight(letterObjects[letter].transform.GetChild(0).GetComponent<TMP_Text>());
        Highlight(fingers[k.group]);

        while (Time.time < closeTime)
            yield return new WaitForFixedUpdate();

        Unhighlight(letterObjects[letter].GetComponent<Image>());
        UnHighlight(letterObjects[letter].transform.GetChild(0).GetComponent<TMP_Text>());
        Unhighlight(fingers[k.group]);

        view.SetActive(false);
        shown = false;
    }

    private void Highlight(Image image)
    {
        var c = image.color;
        image.color = new Color(c.r, c.g, c.b, 1);
    }
    private void Unhighlight(Image image)
    {
        var c = image.color;
        image.color = new Color(c.r, c.g, c.b, transparency);
    }
    private void Highlight(TMP_Text text)
    {
        var c = text.color;
        text.color = new Color(c.r, c.g, c.b, 1);
    }
    private void UnHighlight(TMP_Text text)
    {
        var c = text.color;
        text.color = new Color(c.r, c.g, c.b, transparency);
    }
    public void HideHint()
    {
        if (!shown) return;

        closeTime = 0;
    }

    public struct KeyLetter
    {
        public char letter;
        public int group;

        public KeyLetter(char letter, int group)
        {
            this.letter = letter;
            this.group = group;
        }
    }
}