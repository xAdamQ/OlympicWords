#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
#define USE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.Controls;
#endif

using UnityEngine;
using UnityEngine.Serialization;

namespace UnityTemplateProjects
{
    public class SimpleCameraController : MonoBehaviour
    {
        class CameraState
        {
            public float Yaw;
            public float Pitch;
            public float Roll;
            public float X;
            public float Y;
            public float Z;

            public void SetFromTransform(Transform t)
            {
                Pitch = t.eulerAngles.x;
                Yaw = t.eulerAngles.y;
                Roll = t.eulerAngles.z;
                X = t.position.x;
                Y = t.position.y;
                Z = t.position.z;
            }

            public void Translate(Vector3 translation)
            {
                var rotatedTranslation = Quaternion.Euler(Pitch, Yaw, Roll) * translation;

                X += rotatedTranslation.x;
                Y += rotatedTranslation.y;
                Z += rotatedTranslation.z;
            }

            public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct)
            {
                Yaw = Mathf.Lerp(Yaw, target.Yaw, rotationLerpPct);
                Pitch = Mathf.Lerp(Pitch, target.Pitch, rotationLerpPct);
                Roll = Mathf.Lerp(Roll, target.Roll, rotationLerpPct);
                
                X = Mathf.Lerp(X, target.X, positionLerpPct);
                Y = Mathf.Lerp(Y, target.Y, positionLerpPct);
                Z = Mathf.Lerp(Z, target.Z, positionLerpPct);
            }

            public void UpdateTransform(Transform t)
            {
                t.eulerAngles = new Vector3(Pitch, Yaw, Roll);
                t.position = new Vector3(X, Y, Z);
            }
        }
        
        CameraState mTargetCameraState = new CameraState();
        CameraState mInterpolatingCameraState = new CameraState();

        [FormerlySerializedAs("boost")]
        [Header("Movement Settings")]
        [Tooltip("Exponential boost factor on translation, controllable by mouse wheel.")]
        public float Boost = 3.5f;

        [FormerlySerializedAs("positionLerpTime")] [Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
        public float PositionLerpTime = 0.2f;

        [FormerlySerializedAs("mouseSensitivityCurve")]
        [Header("Rotation Settings")]
        [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
        public AnimationCurve MouseSensitivityCurve = new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));

        [FormerlySerializedAs("rotationLerpTime")] [Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."), Range(0.001f, 1f)]
        public float RotationLerpTime = 0.01f;

        [FormerlySerializedAs("invertY")] [Tooltip("Whether or not to invert our Y axis for mouse input to rotation.")]
        public bool InvertY;

        void OnEnable()
        {
            mTargetCameraState.SetFromTransform(transform);
            mInterpolatingCameraState.SetFromTransform(transform);
        }

        Vector3 GetInputTranslationDirection()
        {
            var direction = new Vector3();
            if (Input.GetKey(KeyCode.W))
            {
                direction += Vector3.forward;
            }
            if (Input.GetKey(KeyCode.S))
            {
                direction += Vector3.back;
            }
            if (Input.GetKey(KeyCode.A))
            {
                direction += Vector3.left;
            }
            if (Input.GetKey(KeyCode.D))
            {
                direction += Vector3.right;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                direction += Vector3.down;
            }
            if (Input.GetKey(KeyCode.E))
            {
                direction += Vector3.up;
            }
            return direction;
        }
        
        void Update()
        {
            var translation = Vector3.zero;

#if ENABLE_LEGACY_INPUT_MANAGER

            // Exit Sample  
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
				#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false; 
				#endif
            }
            // Hide and lock cursor when right mouse button pressed
            if (Input.GetMouseButtonDown(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            // Unlock and show cursor when right mouse button released
            if (Input.GetMouseButtonUp(1))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            // Rotation
            if (Input.GetMouseButton(1))
            {
                var mouseMovement = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y") * (invertY ? 1 : -1));
                
                var mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);

                m_TargetCameraState.yaw += mouseMovement.x * mouseSensitivityFactor;
                m_TargetCameraState.pitch += mouseMovement.y * mouseSensitivityFactor;
            }
            
            // Translation
            translation = GetInputTranslationDirection() * Time.deltaTime;

            // Speed up movement when shift key held
            if (Input.GetKey(KeyCode.LeftShift))
            {
                translation *= 10.0f;
            }

            // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
            boost += Input.mouseScrollDelta.y * 0.2f;
            translation *= Mathf.Pow(2.0f, boost);

#elif USE_INPUT_SYSTEM 
            // TODO: make the new input system work
#endif

            mTargetCameraState.Translate(translation);

            // Framerate-independent interpolation
            // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
            var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / PositionLerpTime) * Time.deltaTime);
            var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / RotationLerpTime) * Time.deltaTime);
            mInterpolatingCameraState.LerpTowards(mTargetCameraState, positionLerpPct, rotationLerpPct);

            mInterpolatingCameraState.UpdateTransform(transform);
        }
    }

}