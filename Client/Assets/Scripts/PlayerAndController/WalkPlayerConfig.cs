using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "WalkPlayerConfig", menuName = "WalkPlayerConfig")]
public class WalkPlayerConfig : ScriptableObject
{
    public float MoveLerp, AnimSpeedMultiplier, AnimRunThreshold, AnimRunLerp, RotationLerp, LetterLookAtThreshold;
}