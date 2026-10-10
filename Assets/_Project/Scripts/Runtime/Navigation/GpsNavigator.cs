using System.Collections.Generic;
using LastFare.Driving;
using UnityEngine;

namespace LastFare.Navigation
{
    /// <summary>
    /// The car's GPS: keeps a shortest route from the vehicle to the current destination and re-routes
    /// as the player drives (including when they ignore it). <see cref="LastFare.UI.GpsView"/> draws it.
    /// </summary>
    public class GpsNavigator : MonoBehaviour
    {
        const float RepathInterval = 0.25f;
        const float TurnAroundPenalty = 60f; // metres; U-turns only when carrying on is much longer

        [SerializeField] CityMap map;
        [SerializeField] Transform vehicle;

        readonly List<Vector3> route = new();
        float repathTimer;

        public CityMap Map => map;
        public Transform Vehicle => vehicle;
        public bool HasDestination { get; private set; }
        public Vector3 Destination { get; private set; }
        public string DestinationLabel { get; private set; } = "";
        public IReadOnlyList<Vector3> Route => route;
        public GpsInstruction Instruction { get; private set; }

        public void SetDestination(Vector3 point, string label)
        {
            HasDestination = true;
            Destination = point;
            DestinationLabel = label;
            Repath();
        }

        public void ClearDestination()
        {
            HasDestination = false;
            DestinationLabel = "";
            route.Clear();
            Instruction = default;
        }

        void Update()
        {
            if (!HasDestination) return;
            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f) Repath();
        }

        void Repath()
        {
            repathTimer = RepathInterval;
            route.Clear();
            route.AddRange(map.Network.FindRoute(vehicle.position, Destination, vehicle.forward, TurnAroundPenalty));
            Instruction = GpsDirections.Describe(route, vehicle.position, vehicle.forward);
        }
    }
}
