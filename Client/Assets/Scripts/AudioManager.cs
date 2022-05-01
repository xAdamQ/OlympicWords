using System;
using UnityEngine;

public class AudioManager : MonoModule<AudioManager>
{
    private AudioSource audioSource;

    [SerializeField] private AudioSource backgroundSource;

    protected override void Awake()
    {
        base.Awake();

        audioSource = GetComponent<AudioSource>();
    }


    private void Start()
    {
        if (SoundState())
            backgroundSource.Play();
    }

    public void Play(AudioClip audioClip)
    {
        if (PlayerPrefs.GetInt("enableSound") == 1) return;

        audioSource.clip = audioClip;
        audioSource.Play();
    }

    public void PlayIfSilent(AudioClip audioClip)
    {
        if (audioSource.isPlaying) return;

        Play(audioClip);
    }


    /// <returns> the new state </returns>
    public bool ToggleSound()
    {
        var state = PlayerPrefs.GetInt("enableSound");
        var newState = state == 0 ? 1 : 0;

        PlayerPrefs.SetInt("enableSound", newState);

        if (newState == 0)
            backgroundSource.Play();
        else
            backgroundSource.Stop();

        return newState == 0;
    }

    public bool SoundState()
    {
        return PlayerPrefs.GetInt("enableSound") == 0;
    }

    public void Vibrate()
    {
#if UNITY_ANDROID
        Handheld.Vibrate();
#endif
    }
}