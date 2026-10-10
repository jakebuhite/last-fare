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

        [SerializeField] RoadMap map;
        [SerializeField] Transform vehicle;

        readonly List<Vector3> route = new();
        readonly List<int> routeRoads = new();
        readonly List<string> routeNames = new();
        float repathTimer;

        public RoadMap Map => map;
        public Transform Vehicle => vehicle;
        public bool HasDestination { get; private set; }
        public Vector3 Destination { get; private set; }
        public string DestinationLabel { get; private set; } = "";
        public IReadOnlyList<Vector3> Route => route;
        public GpsInstruction Instruction { get; private set; }

        /// <summary>The road the car is on (or nearest to).</summary>
        public string CurrentRoad { get; private set; } = "";

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
            routeRoads.Clear();
            routeNames.Clear();
            Instruction = default;
        }

        void Update()
        {
            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f) Repath();
        }

        void Repath()
        {
            repathTimer = RepathInterval;
            CurrentRoad = map.RoadNameAt(vehicle.position);
            if (!HasDestination) return;

            route.Clear();
            route.AddRange(map.Network.FindRoute(vehicle.position, Destination, vehicle.forward, TurnAroundPenalty, routeRoads));
            routeNames.Clear();
            foreach (var road in routeRoads) routeNames.Add(map.Road(road).Name);
            Instruction = GpsDirections.Describe(route, vehicle.position, vehicle.forward, routeNames);
        }
    }
}
