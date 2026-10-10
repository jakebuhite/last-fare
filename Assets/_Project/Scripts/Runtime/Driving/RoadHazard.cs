using System;
using UnityEngine;

namespace LastFare.Driving
{
    /// <summary>
    /// A figure standing in the road. Forces the player to look forward (and away from the mirror).
    /// Stop in front of it and it steps aside; hit it and you pay for it; steer around it and it stays behind you.
    /// </summary>
    public class RoadHazard : MonoBehaviour
    {
        const float HitRadius = 2.2f;
        const float HitSpeed = 1f;
        const float ClearRadius = 14f;
        const float ClearSpeed = 0.6f;
        const float ClearHold = 1.5f;
        const float PassedRadius = 20f;
        const float AheadDot = 0.5f;

        CarController car;
        float stoppedTimer;
        bool approached;

        public bool Armed { get; private set; }

        public event Action Hit;
        public event Action Cleared;
        public event Action Passed;

        public void PlaceAt(Vector3 position, Vector3 facing)
        {
            facing.y = 0f;
            transform.SetPositionAndRotation(position,
                facing.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(facing, Vector3.up) : Quaternion.identity);
        }

        public void Arm(CarController driver)
        {
            car = driver;
            Armed = true;
            approached = false;
            stoppedTimer = 0f;
            gameObject.SetActive(true);
        }

        public void Disarm()
        {
            Armed = false;
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (!Armed || car == null) return;

            var toHazard = transform.position - car.transform.position;
            toHazard.y = 0f;
            var distance = toHazard.magnitude;
            var speed = Mathf.Abs(car.Speed);

            if (distance < HitRadius && speed > HitSpeed)
            {
                Disarm();
                Hit?.Invoke();
                return;
            }

            if (distance < ClearRadius) approached = true;

            var ahead = distance > 0.01f && Vector3.Dot(car.transform.forward, toHazard / distance) > AheadDot;
            if (distance < ClearRadius && ahead && speed < ClearSpeed)
            {
                stoppedTimer += Time.deltaTime;
                if (stoppedTimer >= ClearHold)
                {
                    Disarm();
                    Cleared?.Invoke();
                }
            }
            else
            {
                stoppedTimer = 0f;
            }

            if (approached && distance > PassedRadius)
            {
                Disarm();
                Passed?.Invoke();
            }
        }
    }
}
