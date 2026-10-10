using UnityEngine;

namespace LastFare.Driving
{
    /// <summary>
    /// Arcade car handling: throttle, brake-then-reverse, coasting, handbrake and steering that tightens
    /// at low speed. Plain C# so the feel can be unit tested; <see cref="CarController"/> applies it.
    /// </summary>
    public class ArcadeDrive
    {
        public float MaxSpeed = 16f;
        public float MaxReverseSpeed = 5f;
        public float Acceleration = 4.5f;
        public float BrakeDeceleration = 12f;
        public float CoastDeceleration = 1.5f;
        public float HandbrakeDeceleration = 16f;
        public float Wheelbase = 2.7f;
        public float MaxSteerAngle = 34f;
        public float HighSpeedSteerFactor = 0.4f; // fraction of steering angle left at top speed
        public float SteerResponse = 5f;          // how fast the wheel follows the input, per second

        /// <summary>Signed speed in m/s; negative when reversing. Settable so collisions can bleed it off.</summary>
        public float Speed { get; set; }

        /// <summary>Smoothed steering, -1 (full left) to 1 (full right).</summary>
        public float Steer { get; private set; }

        public bool Braking { get; private set; }

        /// <summary>Advances one step and returns how far the car turns this step, in degrees (positive = right).</summary>
        public float Step(float throttle, float brake, bool handbrake, float steerInput, float dt)
        {
            throttle = Mathf.Clamp01(throttle);
            brake = Mathf.Clamp01(brake);

            float target, rate;
            Braking = false;

            if (handbrake)
            {
                target = 0f;
                rate = HandbrakeDeceleration;
                Braking = true;
            }
            else if (throttle > 0f && Speed < -0.1f)
            {
                target = 0f; // reversing and pressing gas: brake first
                rate = BrakeDeceleration * throttle;
                Braking = true;
            }
            else if (throttle > 0f)
            {
                target = MaxSpeed;
                rate = Acceleration * throttle;
            }
            else if (brake > 0f && Speed > 0.1f)
            {
                target = 0f;
                rate = BrakeDeceleration * brake;
                Braking = true;
            }
            else if (brake > 0f)
            {
                target = -MaxReverseSpeed; // stopped and still holding brake: reverse
                rate = Acceleration * brake;
            }
            else
            {
                target = 0f;
                rate = CoastDeceleration;
            }

            Speed = Mathf.MoveTowards(Speed, target, rate * dt);

            Steer = Mathf.MoveTowards(Steer, Mathf.Clamp(steerInput, -1f, 1f), SteerResponse * dt);
            var speed01 = Mathf.Clamp01(Mathf.Abs(Speed) / MaxSpeed);
            var angle = Steer * MaxSteerAngle * Mathf.Lerp(1f, HighSpeedSteerFactor, speed01);
            var yawRate = Speed * Mathf.Tan(angle * Mathf.Deg2Rad) / Wheelbase; // bicycle model, rad/s
            return yawRate * Mathf.Rad2Deg * dt;
        }
    }
}
