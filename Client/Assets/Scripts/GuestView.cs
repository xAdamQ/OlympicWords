using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class GuestView : MonoBehaviour
{
    [SerializeField] TMP_Text idInput;

    public static void Create()
    {
        Addressables.InstantiateAsync("guestView", Controller.I.canvas);
    }

    public void StartClient()
    {
        Controller.I.TstStartClient(idInput.text);
        // Destroy(gameObject);
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
}