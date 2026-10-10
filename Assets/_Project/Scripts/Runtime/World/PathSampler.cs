using System.Collections.Generic;
using UnityEngine;

namespace LastFare.World
{
    /// <summary>Polyline helpers for laying out roads: smooth curves, gentle arcs and walking along a path.</summary>
    public static class PathSampler
    {
        /// <summary>
        /// A Catmull-Rom curve through <paramref name="controls"/>, resampled to points roughly
        /// <paramref name="spacing"/> metres apart. The first and last control points are kept exactly.
        /// </summary>
        public static List<Vector3> SmoothResample(IReadOnlyList<Vector3> controls, float spacing)
        {
            const int stepsPerSpan = 24;
            var dense = new List<Vector3>();
            for (var i = 0; i < controls.Count - 1; i++)
            {
                var p0 = controls[Mathf.Max(i - 1, 0)];
                var p1 = controls[i];
                var p2 = controls[i + 1];
                var p3 = controls[Mathf.Min(i + 2, controls.Count - 1)];
                for (var s = 0; s < stepsPerSpan; s++)
                    dense.Add(CatmullRom(p0, p1, p2, p3, s / (float)stepsPerSpan));
            }

            dense.Add(controls[controls.Count - 1]);
            return Resample(dense, spacing);
        }

        /// <summary>A road that sets off along <paramref name="direction"/> and bends steadily by <paramref name="bendDegrees"/> in total.</summary>
        public static List<Vector3> Arc(Vector3 start, Vector3 direction, float length, float bendDegrees, float spacing)
        {
            var count = Mathf.Max(1, Mathf.RoundToInt(length / spacing));
            var step = length / count;
            var points = new List<Vector3> { start };
            var heading = Flat(direction).normalized;
            var turnPerStep = bendDegrees / count;
            var p = start;

            for (var i = 0; i < count; i++)
            {
                // Turn half before and half after each step so the arc stays centred on its chords.
                heading = Quaternion.Euler(0f, turnPerStep * 0.5f, 0f) * heading;
                p += heading * step;
                points.Add(p);
                heading = Quaternion.Euler(0f, turnPerStep * 0.5f, 0f) * heading;
            }

            return points;
        }

        /// <summary>Evenly spaced points along a polyline, keeping both ends.</summary>
        public static List<Vector3> Resample(IReadOnlyList<Vector3> polyline, float spacing)
        {
            var total = Length(polyline);
            var count = Mathf.Max(1, Mathf.RoundToInt(total / spacing));
            var result = new List<Vector3>(count + 1);
            for (var i = 0; i < count; i++) result.Add(PointAt(polyline, total * i / count, out _));
            result.Add(polyline[polyline.Count - 1]);
            return result;
        }

        public static float Length(IReadOnlyList<Vector3> polyline)
        {
            var total = 0f;
            for (var i = 1; i < polyline.Count; i++) total += Vector3.Distance(polyline[i - 1], polyline[i]);
            return total;
        }

        /// <summary>The point <paramref name="distance"/> metres along the polyline and the direction of travel there.</summary>
        public static Vector3 PointAt(IReadOnlyList<Vector3> polyline, float distance, out Vector3 tangent)
        {
            tangent = Vector3.forward;
            for (var i = 1; i < polyline.Count; i++)
            {
                var seg = polyline[i] - polyline[i - 1];
                var len = seg.magnitude;
                if (len < 1e-4f) continue;
                tangent = seg / len;
                if (distance <= len) return polyline[i - 1] + tangent * Mathf.Max(0f, distance);
                distance -= len;
            }

            return polyline[polyline.Count - 1];
        }

        public static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>Unit vector to the right of a direction of travel (on the ground plane).</summary>
        public static Vector3 RightOf(Vector3 direction) => Vector3.Cross(Vector3.up, Flat(direction).normalized);

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }
    }
}
