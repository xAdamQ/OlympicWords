using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SoundButton : MonoModule<SoundButton>
{
    [SerializeField] private Sprite enabledSprite, disabledSprite;
    [SerializeField] private Image soundImage;

    public static void Create()
    {
        Create("soundButton", LobbyController.I.Canvas)
            .Forget(e => throw e);
    }

    private void Start()
    {
        soundImage.sprite = AudioManager.I.SoundState()
            ? enabledSprite
            : disabledSprite;
    }

    public void ToggleSound()
    {
        soundImage.sprite = AudioManager.I.ToggleSound() ? enabledSprite : disabledSprite;
    }
}