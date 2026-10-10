using UnityEngine;
using UnityEngine.InputSystem;

namespace LastFare.Player
{
    /// <summary>
    /// Mouse look from the driver's seat. Holding the right mouse button snaps the view to the
    /// rear-view mirror. Scares can lock the view onto a target or shake it.
    /// </summary>
    public class DriverLook : MonoBehaviour
    {
        const float YawLimit = 170f;
        const float PitchUp = 55f;
        const float PitchDown = 60f;

        [SerializeField] float sensitivity = 0.09f;
        [SerializeField] float mirrorSnapSpeed = 10f;

        Camera cam;
        Transform mirrorTarget;
        Transform lockTarget;
        float yaw;
        float pitch;
        float shakeStrength;
        float shakeTime;

        public Camera Camera => cam;
        public bool HoldingMirror { get; private set; }
        public bool InputEnabled { get; set; } = true;

        public void Init(Camera camera, Transform mirror)
        {
            cam = camera;
            mirrorTarget = mirror;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void LockOn(Transform target) => lockTarget = target;

        public void Shake(float strength, float seconds)
        {
            shakeStrength = strength;
            shakeTime = seconds;
        }

        /// <summary>Angle in degrees between where the player is looking and a world point.</summary>
        public float AngleTo(Vector3 worldPoint)
        {
            var toPoint = worldPoint - cam.transform.position;
            return Vector3.Angle(cam.transform.forward, toPoint);
        }

        void Update()
        {
            if (cam == null) return;

            var mouse = Mouse.current;
            HoldingMirror = InputEnabled && lockTarget == null && mouse != null && mouse.rightButton.isPressed;

            if (lockTarget != null)
            {
                AimAt(lockTarget.position, 20f);
            }
            else if (HoldingMirror && mirrorTarget != null)
            {
                AimAt(mirrorTarget.position, mirrorSnapSpeed);
            }
            else if (InputEnabled && mouse != null)
            {
                var delta = mouse.delta.ReadValue() * sensitivity;
                yaw = Mathf.Clamp(yaw + delta.x, -YawLimit, YawLimit);
                pitch = Mathf.Clamp(pitch - delta.y, -PitchUp, PitchDown);
            }

            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            ApplyShake();
        }

        void AimAt(Vector3 worldPoint, float speed)
        {
            var parent = transform.parent;
            var dir = parent != null
                ? parent.InverseTransformDirection(worldPoint - transform.position)
                : worldPoint - transform.position;

            var targetYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            var targetPitch = -Mathf.Asin(Mathf.Clamp(dir.y / Mathf.Max(dir.magnitude, 0.001f), -1f, 1f)) * Mathf.Rad2Deg;

            var t = 1f - Mathf.Exp(-speed * Time.deltaTime);
            yaw = Mathf.LerpAngle(yaw, targetYaw, t);
            pitch = Mathf.Lerp(pitch, targetPitch, t);
        }

        void ApplyShake()
        {
            if (shakeTime > 0f)
            {
                shakeTime -= Time.deltaTime;
                cam.transform.localPosition = Random.insideUnitSphere * (shakeStrength * Mathf.Clamp01(shakeTime));
            }
            else if (cam.transform.localPosition != Vector3.zero)
            {
                cam.transform.localPosition = Vector3.zero;
            }
        }
    }
}
