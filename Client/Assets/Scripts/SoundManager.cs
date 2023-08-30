using System;
using UnityEngine;

public class SoundManager : MonoModule<SoundManager>
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] letterSounds;

    protected override void Awake()
    {
        base.Awake();

        DontDestroyOnLoad(gameObject);
    }

    private int direction = 1;
    private int index = -1;

    public void PlayLetterSound()
    {
        index += direction;

        if (index == letterSounds.Length || index == -1)
        {
            direction *= -1;
            index += direction;
        }

        audioSource.PlayOneShot(letterSounds[index]);
    }
}