using UnityEngine;

public abstract class LetterController<TPlayer> : GraphController<TPlayer> where TPlayer : GraphPlayer
{
    protected override Transform CameraTarget => Player.currentLetter.transform;
}