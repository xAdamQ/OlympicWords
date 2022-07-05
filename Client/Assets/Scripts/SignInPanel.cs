using TMPro;
using UnityEngine;

public class SignInPanel : MonoModule<SignInPanel>
{
    [SerializeField] private GameObject guestViewPrefab;

    protected override void Awake()
    {
        base.Awake();
        Instantiate(guestViewPrefab, transform);
    }
    //there will be other ways than guest to logon
}