using System;
using UnityEngine;
using UnityEngine.UI;

public class RadioSet : MonoBehaviour
{
    [SerializeField] private Color ChosenColor, NormalColor;

    private Button[] buttons;
    public int CurrentChoice;

    private void Start()
    {
        InitButtons();
        RefreshColors();
    }

    private void InitButtons()
    {
        buttons = new Button[transform.childCount];
        var i = 0;
        foreach (Transform child in transform)
        {
            var button = child.GetComponent<Button>();
            buttons[i] = button;
            var choice = i;
            button.onClick.AddListener(() => Choose(choice));
            i++;
        }
    }

    protected virtual void Choose(int choice)
    {
        CurrentChoice = choice;
        RefreshColors();
    }

    private void RefreshColors()
    {
        buttons.ForEach(c => c.GetComponent<Image>().color = NormalColor);
        buttons[CurrentChoice].GetComponent<Image>().color = ChosenColor;
    }
}