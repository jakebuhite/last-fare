using System;
using UnityEngine;

namespace LastFare.Driving
{
    /// <summary>
    /// A closed loop of road described by a dense polyline. The car is positioned by
    /// distance along the loop, so driving needs no physics. Plain C# for easy testing.
    /// </summary>
    public class RoutePath
    {
        readonly Vector3[] points;
        readonly float[] segmentStart; // segmentStart[i] = distance at points[i]; last entry = total length

        public float Length { get; }
        public int PointCount => points.Length;

        public RoutePath(Vector3[] loopPoints)
        {
            if (loopPoints == null || loopPoints.Length < 3)
                throw new ArgumentException("A loop needs at least 3 points.", nameof(loopPoints));

            points = (Vector3[])loopPoints.Clone();
            segmentStart = new float[points.Length + 1];

            var total = 0f;
            for (var i = 0; i < points.Length; i++)
            {
                segmentStart[i] = total;
                total += Vector3.Distance(points[i], points[(i + 1) % points.Length]);
            }

            segmentStart[points.Length] = total;
            Length = total;
        }

        /// <summary>Builds a wobbly circular loop around the origin on the XZ plane.</summary>
        public static RoutePath CreateLoop(float radius, int samples = 240, float wobble = 0.18f)
        {
            var pts = new Vector3[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)samples * Mathf.PI * 2f;
                var r = radius * (1f + wobble * Mathf.Sin(3f * t) + wobble * 0.4f * Mathf.Sin(5f * t + 1f));
                pts[i] = new Vector3(Mathf.Cos(t) * r, 0f, Mathf.Sin(t) * r);
            }

            return new RoutePath(pts);
        }

        public Vector3 GetPoint(int index) => points[((index % points.Length) + points.Length) % points.Length];

        /// <summary>Position and travel direction at a distance along the loop (wraps).</summary>
        public void Evaluate(float distance, out Vector3 position, out Vector3 forward)
        {
            var d = Mathf.Repeat(distance, Length);

            var index = Array.BinarySearch(segmentStart, 0, points.Length, d);
            if (index < 0) index = ~index - 1;
            index = Mathf.Clamp(index, 0, points.Length - 1);

            var a = points[index];
            var b = points[(index + 1) % points.Length];
            var segLength = segmentStart[index + 1] - segmentStart[index];
            var t = segLength > 0f ? (d - segmentStart[index]) / segLength : 0f;

            position = Vector3.Lerp(a, b, t);
            forward = (b - a).normalized;
        }

        /// <summary>Point to the right of the road centre line at a distance (negative offset = left).</summary>
        public Vector3 OffsetPoint(float distance, float rightOffset)
        {
            Evaluate(distance, out var pos, out var fwd);
            return pos + Vector3.Cross(Vector3.up, fwd) * rightOffset;
        }
    }
}
