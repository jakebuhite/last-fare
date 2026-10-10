using System;
using UnityEngine;

namespace LastFare.Player
{
    /// <summary>
    /// The driver's star rating. Doubles as the health bar: hit the deactivation
    /// threshold and the shift (run) is over. Plain C# so it is easy to unit test.
    /// </summary>
    public class DriverRating
    {
        public const float Max = 5f;

        public float Value { get; private set; }
        public float DeactivationThreshold { get; }
        public bool IsDeactivated => Value <= DeactivationThreshold;

        public event Action<float> Changed;
        public event Action Deactivated;

        public DriverRating(float startingValue = 4.9f, float deactivationThreshold = 3.5f)
        {
            Value = Mathf.Clamp(startingValue, 0f, Max);
            DeactivationThreshold = deactivationThreshold;
        }

        public void Apply(float delta)
        {
            if (IsDeactivated || Mathf.Approximately(delta, 0f)) return;

            Value = Mathf.Clamp(Value + delta, 0f, Max);
            Changed?.Invoke(Value);

            if (IsDeactivated) Deactivated?.Invoke();
        }
    }
}
