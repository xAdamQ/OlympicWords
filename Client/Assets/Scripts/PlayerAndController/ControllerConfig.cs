using UnityEngine;

[CreateAssetMenu(fileName = "ControllerConfig", menuName = "ControllerConfig", order = 0)]
public class ControllerConfig : ScriptableObject
{
    public Material WordHighlightMat;
    public Material FadeMaterial;
    public float JumpZoomCoefficient;
    public Vector3 CameraOffset;
    public float CameraMoveSmoothing;
    public float CameraLookSmoothing;
}