using UnityEngine;

[CreateAssetMenu(fileName = "ControllerConfig", menuName = "ControllerConfig", order = 0)]
public class ControllerConfig : ScriptableObject
{
    public Material WordHighlightMat;
    public Material FadeMaterial;
    public Vector3 CameraOffset;
    public float CameraMoveSmoothing;
    public float CameraLookSmoothing;
}

[CreateAssetMenu(fileName = "JumpControllerConfig", menuName = "JumpControllerConfig", order = 0)]
public class JumpControllerConfig : ScriptableObject
{
    public float JumpZoomCoefficient;
}