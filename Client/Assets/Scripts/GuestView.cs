using System;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class GuestView : MonoBehaviour
{
    [SerializeField] TMP_Text idInput;

    public void StartWithId()
    {
        NetManager.I.ConnectToServer(fbigToken: idInput.text, name: "guest", demo: true);
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

    public void StartWithRandomId()
    {
        NetManager.I.ConnectToServer(fbigToken: Random.Range(0, int.MaxValue).ToString(), name: "guest", demo: true);
    }
}