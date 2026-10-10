using System;
using UnityEngine;

namespace LastFare.Driving
{
    /// <summary>
    /// A figure standing in the road. Forces the player to look forward (and away from the mirror).
    /// Stop in front of it and it steps aside; hit it and you pay for it.
    /// </summary>
    public class RoadHazard : MonoBehaviour
    {
        const float HitGap = 3f;
        const float HitSpeed = 1f;
        const float ClearGap = 14f;
        const float ClearSpeed = 0.6f;
        const float ClearHold = 1.5f;

        [SerializeField] float distance;

        CarController car;
        float stoppedTimer;

        public bool Armed { get; private set; }

        public event Action Hit;
        public event Action Cleared;

        public void Place(RoutePath route, float distanceAlongRoute)
        {
            distance = distanceAlongRoute;
            route.Evaluate(distanceAlongRoute, out var pos, out var fwd);
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-fwd, Vector3.up));
            gameObject.SetActive(false);
        }

        public void Arm(CarController driver)
        {
            car = driver;
            Armed = true;
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

            var gap = distance - car.Distance;

            if ((gap < HitGap && car.Speed > HitSpeed) || gap < 0.5f)
            {
                Disarm();
                Hit?.Invoke();
                return;
            }

            if (gap < ClearGap && car.Speed < ClearSpeed)
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
        }
    }
}
