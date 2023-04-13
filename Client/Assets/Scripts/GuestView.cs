using System;
using Cysharp.Threading.Tasks;
using Shared;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class GuestView : MonoModule<GuestView>
{
    [SerializeField] private TMP_Text idInput;
    [SerializeField] private TMP_InputField accessTokenField;

    [SerializeField] private string[] addresses;
    [SerializeField] private ChoiceButton serverAddressChoice;
    [SerializeField] private TMP_InputField customAddress;
    [SerializeField] private TMP_Text chosenAddressText;

    protected override void Awake()
    {
        base.Awake();
        NetManager.I.SelectedAddress = addresses[0];
        serverAddressChoice.ChoiceChanged += _ => chosenAddressText.text = GetServerAddress();
    }

    public string GetServerAddress()
    {
        return serverAddressChoice.CurrentChoice >= addresses.Length
            ? customAddress.text
            : addresses[serverAddressChoice.CurrentChoice];
    }

    public void StartWithId()
    {
        NetManager.I.BlockingLogin(idInput.text, ProviderType.Guest);
    }

    public void AddChar(string chr)
    {
        if (idInput.text.Length >= 5) return;

        idInput.text += chr;
    }

    public void ClearInput()
    {
        idInput.text = "";
    }

    public void FacebookConnect()
    {
        NetManager.I.BlockingLogin(accessTokenField.text, ProviderType.Facebook);
    }

    public void StartWithRandomId()
    {
        var id = Random.Range(10000, 99999).ToString();
        NetManager.I.BlockingLogin(id, ProviderType.Guest);

    }
}