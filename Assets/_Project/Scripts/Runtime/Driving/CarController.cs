using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastFare.Driving
{
    /// <summary>
    /// Free-driving car. W/S accelerate and brake (hold S when stopped to reverse), A/D steer, Space is the
    /// handbrake. Handling comes from <see cref="ArcadeDrive"/>; the Rigidbody only resolves collisions.
    /// Off the tarmac (grass, sidewalks, lawns) the car is limited to a crawl.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [SerializeField] float maxSpeed = 28f;
        [SerializeField] float offRoadMaxSpeed = 9f;
        [SerializeField] RoadMap roads;
        [SerializeField] float maxReverseSpeed = 5f;
        [SerializeField] float acceleration = 5.5f;
        [SerializeField] float brakeDeceleration = 12f;
        [SerializeField] float maxSteerAngle = 34f;
        [Tooltip("Impact speed (m/s) that counts as a crash.")]
        [SerializeField] float crashSpeed = 3f;

        Rigidbody body;
        ArcadeDrive drive;
        Vector3 commandedDirection;

        /// <summary>Signed speed in m/s; negative when reversing.</summary>
        public float Speed => drive?.Speed ?? 0f;
        public float MaxSpeed => maxSpeed;
        /// <summary>Smoothed steering, -1 (full left) to 1 (full right).</summary>
        public float Steer => drive?.Steer ?? 0f;
        public bool Braking => drive != null && drive.Braking;
        public bool Reversing => Speed < -0.1f;
        public bool OffRoad { get; private set; }
        public bool PlayerControl { get; set; } = true;

        /// <summary>Raised when the car hits something hard enough. Argument: impact speed in m/s.</summary>
        public event Action<float> Crashed;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            drive = new ArcadeDrive
            {
                MaxSpeed = maxSpeed,
                MaxReverseSpeed = maxReverseSpeed,
                Acceleration = acceleration,
                BrakeDeceleration = brakeDeceleration,
                MaxSteerAngle = maxSteerAngle
            };
            commandedDirection = transform.forward;
        }

        public bool IsStoppedNear(Vector3 point, float radius)
        {
            var offset = point - transform.position;
            offset.y = 0f;
            return offset.magnitude <= radius && Mathf.Abs(Speed) < 0.5f;
        }

        /// <summary>Immediately cut speed and take control away from the player (used by scares).</summary>
        public void HaltNow()
        {
            drive.Speed = 0f;
            PlayerControl = false;
            body.linearVelocity = Vector3.zero;
        }

        void FixedUpdate()
        {
            // If a collision slowed us, keep that: never drive faster than physics actually let us move.
            var actual = Vector3.Dot(body.linearVelocity, commandedDirection);
            if (Mathf.Abs(actual) < Mathf.Abs(drive.Speed)) drive.Speed = actual;

            OffRoad = roads != null && !roads.IsOnRoad(body.position, 0.8f);
            drive.MaxSpeed = OffRoad ? offRoadMaxSpeed : maxSpeed;

            ReadInput(out var throttle, out var brake, out var handbrake, out var steer);
            var yaw = drive.Step(throttle, brake, handbrake, steer, Time.fixedDeltaTime);

            var rotation = body.rotation * Quaternion.Euler(0f, yaw, 0f);
            body.MoveRotation(rotation);
            commandedDirection = rotation * Vector3.forward;
            body.linearVelocity = commandedDirection * drive.Speed;
        }

        void ReadInput(out float throttle, out float brake, out bool handbrake, out float steer)
        {
            throttle = brake = steer = 0f;
            handbrake = !PlayerControl;

            var kb = Keyboard.current;
            if (kb == null || !PlayerControl) return;

            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) throttle = 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) brake = 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steer -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steer += 1f;
            handbrake = kb.spaceKey.isPressed;
        }

        void OnCollisionEnter(Collision collision)
        {
            var impact = collision.relativeVelocity.magnitude;
            if (impact >= crashSpeed) Crashed?.Invoke(impact);
        }
    }
}
