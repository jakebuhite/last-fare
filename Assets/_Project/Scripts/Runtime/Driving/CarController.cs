using UnityEngine;
using UnityEngine.InputSystem;

namespace LastFare.Driving
{
    /// <summary>
    /// Semi-on-rails car. It follows a <see cref="RoutePath"/>; the player sets a cruise speed
    /// (W/S), can slam the brakes (Space), and the car eases to a stop at pickup/dropoff points.
    /// </summary>
    public class CarController : MonoBehaviour
    {
        const float StopComfortDecel = 4f;
        const float StopSnapDistance = 0.05f;

        [SerializeField] float maxSpeed = 12f;
        [SerializeField] float acceleration = 3.5f;
        [SerializeField] float cruiseDecel = 5f;
        [SerializeField] float brakeDecel = 11f;
        [SerializeField] float cruiseChangeRate = 5f;

        RoutePath route;
        bool hasStop;
        float stopAt;

        public float Distance { get; private set; }
        public float Speed { get; private set; }
        public float TargetSpeed { get; private set; }
        public float MaxSpeed => maxSpeed;
        public bool PlayerControl { get; set; } = true;
        public bool Braking { get; private set; }

        public void Init(RoutePath path, float startDistance)
        {
            route = path;
            Distance = startDistance;
            Speed = 0f;
            TargetSpeed = 0f;
            ApplyTransform();
        }

        public void StopAt(float distance)
        {
            hasStop = true;
            stopAt = distance;
        }

        public void ClearStop() => hasStop = false;

        public float RemainingToStop => hasStop ? stopAt - Distance : float.PositiveInfinity;

        public bool IsStoppedAtTarget => hasStop && RemainingToStop <= 0.4f && Speed < 0.15f;

        /// <summary>Immediately cut speed (used by scares).</summary>
        public void HaltNow()
        {
            Speed = 0f;
            TargetSpeed = 0f;
            PlayerControl = false;
        }

        void Update()
        {
            if (route == null) return;

            ReadInput(Time.deltaTime);

            var desired = TargetSpeed;

            if (hasStop)
            {
                var remaining = stopAt - Distance;
                // Close to the stop, keep creeping so the player doesn't have to hover on the pedal.
                if (remaining < 10f && !Braking) desired = Mathf.Max(desired, 2f);
                var limit = Mathf.Sqrt(2f * StopComfortDecel * Mathf.Max(0f, remaining));
                desired = Mathf.Min(desired, limit);
            }

            var rate = desired >= Speed ? acceleration : (Braking ? brakeDecel : cruiseDecel);
            Speed = Mathf.MoveTowards(Speed, desired, rate * Time.deltaTime);
            Distance += Speed * Time.deltaTime;

            if (hasStop && stopAt - Distance <= StopSnapDistance)
            {
                Distance = Mathf.Min(Distance, stopAt);
                Speed = 0f;
            }

            ApplyTransform();
        }

        void ReadInput(float dt)
        {
            Braking = false;
            var kb = Keyboard.current;
            if (kb == null || !PlayerControl) return;

            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) TargetSpeed += cruiseChangeRate * dt;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) TargetSpeed -= cruiseChangeRate * dt;
            if (kb.spaceKey.isPressed)
            {
                Braking = true;
                TargetSpeed = 0f;
            }

            TargetSpeed = Mathf.Clamp(TargetSpeed, 0f, maxSpeed);
        }

        void ApplyTransform()
        {
            route.Evaluate(Distance, out var pos, out var fwd);
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd, Vector3.up));
        }
    }
}
