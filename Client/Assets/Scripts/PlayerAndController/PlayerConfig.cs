using UnityEngine;
[CreateAssetMenu(fileName = "PlayerConfig", menuName = "PlayerConfig", order = 0)]
public class PlayerConfig : ScriptableObject
{
    public float JumpTime, AutomationSpeedUp, JetJumpSlowDown, JetpackTime;
}