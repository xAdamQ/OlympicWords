using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Popup : MonoBehaviour
{
    private static Popup instance;

    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button choiceButtonPrefab; //for both cancellation and dismiss
    [SerializeField] private Transform choiceContainer;

    public static void Show(string message, IEnumerable<(string, Action)> choices)
    {
        if (instance) Destroy(instance.gameObject);

        instance = Instantiate(Controller.I.References.PopupPrefab, Controller.I.canvas)
            .GetComponent<Popup>();

        instance.Init(message, choices);
    }

    private void Init(string message, IEnumerable<(string text, Action action)> choices)
    {
        instance.messageText.text = message;

        foreach (var (text, action) in choices)
        {
            var button = Instantiate(instance.choiceButtonPrefab, choiceContainer);
            button.GetComponentInChildren<TMP_Text>().text = text;
            button.onClick.AddListener(() => action());
        }
    }
}