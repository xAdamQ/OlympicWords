using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Popup : MonoBehaviour
{
    private static Popup instance;

    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button choiceButtonPrefab; //for both cancellation and dismiss
    [SerializeField] private Transform choiceContainer;

    private List<Button> choiceButtons = new();

    private static void ShowBase()
    {
        if (instance) Destroy(instance.gameObject);

        instance = Instantiate(Controller.I.References.PopupPrefab, Controller.I.canvas)
            .GetComponent<Popup>();
    }
    public static void Show(string message, IEnumerable<(string, Action)> choices)
    {
        ShowBase();
        instance.Init(message, choices);
    }
    public static void Show(string message, IEnumerable<(string, UniTask)> choices)
    {
        ShowBase();
        instance.Init(message, choices);
    }


    private void Init(string message, IEnumerable<(string text, Action action)> choices)
    {
        instance.messageText.text = message;

        foreach (var (text, action) in choices)
        {
            var button = Instantiate(instance.choiceButtonPrefab, choiceContainer);
            button.GetComponentInChildren<TMP_Text>().text = text;
            button.onClick.AddListener(() => OnClick(action));
            choiceButtons.Add(button);
        }
    }
    private void Init(string message, IEnumerable<(string text, UniTask uniTask)> choices)
    {
        instance.messageText.text = message;

        foreach (var (text, uniTask) in choices)
        {
            var button = Instantiate(instance.choiceButtonPrefab, choiceContainer);
            button.GetComponentInChildren<TMP_Text>().text = text;
            button.onClick.AddListener(() => OnClick(uniTask));
            choiceButtons.Add(button);
        }
    }

    private void OnClick(Action action)
    {
        action();
        Destroy(gameObject);
    }

    private void OnClick(UniTask action)
    {
        UniTask.Create(async () =>
        {
            choiceButtons.ForEach(b => b.interactable = false);
            await action;
            Destroy(gameObject);
        }).Forget(e => throw e);
    }
}