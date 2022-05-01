using System;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceButton : MonoBehaviour
{
    [SerializeField] private Image[] choiceIndicators;

    [SerializeField] private int startChoice;

    [HideInInspector] public int CurrentChoice;

    [SerializeField] private Color choiceColor = Color.red, unselectedColor = Color.white;
    private void Start()
    {
        if (startChoice != -1)
            SetChoice(startChoice);
    }

    public void SetChoice(int choice)
    {
        CurrentChoice = choice;

        foreach (var ci in choiceIndicators)
            ci.color = unselectedColor;

        choiceIndicators[CurrentChoice].color = choiceColor;

        ChoiceChanged?.Invoke(CurrentChoice);
    }

    public virtual void NextChoice()
    {
        choiceIndicators[CurrentChoice].color = unselectedColor;

        CurrentChoice = ++CurrentChoice % choiceIndicators.Length;

        choiceIndicators[CurrentChoice].color = choiceColor;

        ChoiceChanged?.Invoke(CurrentChoice);
    }

    public event Action<int> ChoiceChanged;
}