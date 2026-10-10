using System.Collections.Generic;
using UnityEngine;

namespace LastFare.Navigation
{
    public enum Maneuver { Arrive, TurnLeft, TurnRight, UTurn, Continue }

    /// <summary>The next thing the GPS tells the driver to do.</summary>
    public readonly struct GpsInstruction
    {
        public readonly Maneuver Maneuver;
        public readonly float DistanceToManeuver;
        public readonly float RemainingMeters;
        public readonly string Onto; // road the maneuver leads onto, if known

        public GpsInstruction(Maneuver maneuver, float distanceToManeuver, float remainingMeters, string onto = null)
        {
            Maneuver = maneuver;
            DistanceToManeuver = distanceToManeuver;
            RemainingMeters = remainingMeters;
            Onto = onto;
        }

        public string Action => Maneuver switch
        {
            Maneuver.TurnLeft => "Turn left",
            Maneuver.TurnRight => "Turn right",
            Maneuver.UTurn => "Make a U-turn",
            Maneuver.Continue => "Continue",
            _ => "Destination ahead"
        };

        public string When => DistanceToManeuver < 15f ? "now" : $"in {GpsDirections.FormatDistance(DistanceToManeuver)}";
    }

    /// <summary>Turn-by-turn directions along a route polyline. Plain C# for unit testing.</summary>
    public static class GpsDirections
    {
        const float CornerAngle = 30f;   // smaller bends aren't announced
        const float UTurnAngle = 150f;
        const float MinSegment = 0.5f;
        const float WrongWayDot = -0.3f; // facing more than ~107 degrees away from the route

        public static float Length(IReadOnlyList<Vector3> route)
        {
            var total = 0f;
            for (var i = 1; i < route.Count; i++) total += Vector3.Distance(route[i - 1], route[i]);
            return total;
        }

        /// <summary>"850 m" under a kilometre (rounded to 10 m), "1.2 km" above.</summary>
        public static string FormatDistance(float meters) => meters < 1000f
            ? $"{Mathf.Round(meters / 10f) * 10f:0} m"
            : $"{meters / 1000f:0.0} km";

        /// <summary>
        /// What to do next, for a driver at <paramref name="position"/> facing <paramref name="heading"/>.
        /// <paramref name="segmentNames"/> (optional) names the road of each route segment; a change of name
        /// without a real turn is announced as "Continue onto ...".
        /// </summary>
        public static GpsInstruction Describe(IReadOnlyList<Vector3> route, Vector3 position, Vector3 heading,
            IReadOnlyList<string> segmentNames = null)
        {
            if (route == null || route.Count == 0) return new GpsInstruction(Maneuver.Arrive, 0f, 0f);

            var toStart = Flat(route[0] - position).magnitude;
            var remaining = toStart + Length(route);

            // First real direction of travel along the route.
            var index = 1;
            while (index < route.Count && Flat(route[index] - route[index - 1]).magnitude < MinSegment) index++;
            if (index >= route.Count) return new GpsInstruction(Maneuver.Arrive, remaining, remaining);

            var dir = Flat(route[index] - route[index - 1]).normalized;
            if (Vector3.Dot(Flat(heading).normalized, dir) < WrongWayDot)
                return new GpsInstruction(Maneuver.UTurn, 0f, remaining);

            var along = toStart;
            for (var i = 1; i < index; i++) along += Vector3.Distance(route[i - 1], route[i]);
            var currentName = NameOf(segmentNames, index - 1);

            for (var i = index; i < route.Count - 1; i++)
            {
                along += Vector3.Distance(route[i - 1], route[i]);
                var next = Flat(route[i + 1] - route[i]);
                if (next.magnitude < MinSegment) continue;

                var onto = NameOf(segmentNames, i);
                var angle = Vector3.SignedAngle(dir, next, Vector3.up);
                if (Mathf.Abs(angle) >= UTurnAngle) return new GpsInstruction(Maneuver.UTurn, along, remaining, onto);
                if (Mathf.Abs(angle) >= CornerAngle)
                    return new GpsInstruction(angle > 0f ? Maneuver.TurnRight : Maneuver.TurnLeft, along, remaining, onto);
                if (onto != null && currentName != null && onto != currentName)
                    return new GpsInstruction(Maneuver.Continue, along, remaining, onto);

                dir = next.normalized;
                currentName = onto ?? currentName;
            }

            return new GpsInstruction(Maneuver.Arrive, remaining, remaining);
        }

        /// <summary>The point <paramref name="distance"/> metres along the route, and the travel direction there.</summary>
        public static Vector3 PointAlong(IReadOnlyList<Vector3> route, float distance, out Vector3 direction)
        {
            direction = Vector3.forward;
            if (route == null || route.Count == 0) return Vector3.zero;

            for (var i = 1; i < route.Count; i++)
            {
                var seg = route[i] - route[i - 1];
                var len = seg.magnitude;
                if (len < 1e-4f) continue;
                direction = seg / len;
                if (distance <= len) return route[i - 1] + direction * distance;
                distance -= len;
            }

            return route[route.Count - 1];
        }

        static string NameOf(IReadOnlyList<string> names, int segment) =>
            names != null && segment >= 0 && segment < names.Count ? names[segment] : null;

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
