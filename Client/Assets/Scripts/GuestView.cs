using System;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class GuestView : MonoBehaviour
{
    [SerializeField] TMP_Text idInput;
    [SerializeField] private TMP_InputField accessTokenField;

    public void StartWithId()
    {
        NetManager.I.ConnectToServer(idInput.text, "demo");
    }

    public void addChar(string chr)
    {
        if (idInput.text.Length >= 5) return;

        idInput.text += chr;
    }

    public void clearInput()
    {
        idInput.text = "";
    }

    public void FacebookConnect()
    {
        NetManager.I.ConnectToServer(accessTokenField.text, "facebook");
    }

    public void StartWithRandomId()
    {
        NetManager.I.ConnectToServer(Random.Range(0, int.MaxValue).ToString(), "demo");
    }
}