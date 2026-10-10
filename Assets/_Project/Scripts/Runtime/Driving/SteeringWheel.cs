using UnityEngine;

namespace LastFare.Driving
{
    /// <summary>Turns the steering wheel model to match the car's steering.</summary>
    public class SteeringWheel : MonoBehaviour
    {
        [SerializeField] CarController car;
        [Tooltip("Wheel rotation at full lock, in degrees.")]
        [SerializeField] float maxAngle = 120f;

        Quaternion rest;

        void Awake() => rest = transform.localRotation;

        void LateUpdate()
        {
            if (car == null) return;
            // Negative about the column axis is clockwise from the driver's seat, i.e. steering right.
            transform.localRotation = rest * Quaternion.Euler(0f, 0f, -car.Steer * maxAngle);
        }
    }
}
