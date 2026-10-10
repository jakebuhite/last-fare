using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastFare.Driving
{
    /// <summary>
    /// The drivable roads of the open map: intersections joined by straight road segments.
    /// Finds the shortest drive between any two points on the roads. Plain C# so routing can be unit tested.
    /// </summary>
    public class RoadNetwork
    {
        readonly Vector3[] nodes;
        readonly (int A, int B)[] edges;
        readonly List<int>[] edgesAtNode;

        public RoadNetwork(Vector3[] nodePositions, (int A, int B)[] roadSegments)
        {
            if (nodePositions == null || nodePositions.Length < 2)
                throw new ArgumentException("A road network needs at least 2 intersections.", nameof(nodePositions));
            if (roadSegments == null || roadSegments.Length == 0)
                throw new ArgumentException("A road network needs at least 1 road.", nameof(roadSegments));

            nodes = (Vector3[])nodePositions.Clone();
            edges = ((int, int)[])roadSegments.Clone();
            edgesAtNode = new List<int>[nodes.Length];
            for (var i = 0; i < nodes.Length; i++) edgesAtNode[i] = new List<int>();

            for (var e = 0; e < edges.Length; e++)
            {
                var (a, b) = edges[e];
                if (a < 0 || a >= nodes.Length || b < 0 || b >= nodes.Length || a == b)
                    throw new ArgumentException($"Road {e} joins invalid intersections {a} and {b}.", nameof(roadSegments));
                edgesAtNode[a].Add(e);
                edgesAtNode[b].Add(e);
            }
        }

        /// <summary>A city grid of <paramref name="columns"/> x <paramref name="rows"/> intersections. Node id = x + z * columns.</summary>
        public static RoadNetwork Grid(int columns, int rows, float spacing, Vector3 origin)
        {
            if (columns < 2 || rows < 2) throw new ArgumentException("A grid needs at least 2 x 2 intersections.");

            var points = new Vector3[columns * rows];
            var roads = new List<(int, int)>();
            for (var z = 0; z < rows; z++)
            {
                for (var x = 0; x < columns; x++)
                {
                    var id = x + z * columns;
                    points[id] = origin + new Vector3(x * spacing, 0f, z * spacing);
                    if (x + 1 < columns) roads.Add((id, id + 1));
                    if (z + 1 < rows) roads.Add((id, id + columns));
                }
            }

            return new RoadNetwork(points, roads.ToArray());
        }

        public int NodeCount => nodes.Length;
        public int EdgeCount => edges.Length;
        public Vector3 Node(int index) => nodes[index];
        public (int A, int B) Edge(int index) => edges[index];

        /// <summary>The nearest point on any road to <paramref name="point"/>.</summary>
        public Vector3 ClosestPoint(Vector3 point, out int edge)
        {
            edge = 0;
            var best = Vector3.zero;
            var bestSqr = float.PositiveInfinity;

            for (var e = 0; e < edges.Length; e++)
            {
                var p = ClosestOnSegment(point, nodes[edges[e].A], nodes[edges[e].B]);
                var sqr = (p - point).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = p;
                    edge = e;
                }
            }

            return best;
        }

        /// <summary>
        /// Shortest drive from <paramref name="from"/> to <paramref name="to"/>, both snapped onto the roads.
        /// The result starts and ends at those snapped points, with every intersection passed in between.
        /// Routes that start by turning around (against <paramref name="heading"/>) cost an extra
        /// <paramref name="turnAroundPenalty"/> metres, so the GPS prefers carrying on when it's close.
        /// </summary>
        public List<Vector3> FindRoute(Vector3 from, Vector3 to, Vector3 heading = default, float turnAroundPenalty = 0f)
        {
            var start = ClosestPoint(from, out var startEdge);
            var end = ClosestPoint(to, out var endEdge);
            if (startEdge == endEdge) return new List<Vector3> { start, end };

            var dist = new float[nodes.Length];
            var prev = new int[nodes.Length];
            var done = new bool[nodes.Length];
            for (var i = 0; i < nodes.Length; i++)
            {
                dist[i] = float.PositiveInfinity;
                prev[i] = -1;
            }

            var (sa, sb) = edges[startEdge];
            dist[sa] = Vector3.Distance(start, nodes[sa]);
            dist[sb] = Vector3.Distance(start, nodes[sb]);
            if (heading != Vector3.zero)
            {
                if (Vector3.Dot(nodes[sa] - start, heading) < 0f) dist[sa] += turnAroundPenalty;
                if (Vector3.Dot(nodes[sb] - start, heading) < 0f) dist[sb] += turnAroundPenalty;
            }

            // Dijkstra. Maps are small (tens of intersections), so a linear scan beats a heap here.
            for (var iter = 0; iter < nodes.Length; iter++)
            {
                var u = -1;
                for (var i = 0; i < nodes.Length; i++)
                    if (!done[i] && (u < 0 || dist[i] < dist[u])) u = i;
                if (u < 0 || float.IsPositiveInfinity(dist[u])) break;
                done[u] = true;

                foreach (var e in edgesAtNode[u])
                {
                    // Both ends of the starting street are already costed; driving back along it would
                    // only be a way to sneak a U-turn past the penalty.
                    if (e == startEdge) continue;
                    var v = edges[e].A == u ? edges[e].B : edges[e].A;
                    var d = dist[u] + Vector3.Distance(nodes[u], nodes[v]);
                    if (d < dist[v])
                    {
                        dist[v] = d;
                        prev[v] = u;
                    }
                }
            }

            var (ea, eb) = edges[endEdge];
            var viaA = dist[ea] + Vector3.Distance(nodes[ea], end);
            var viaB = dist[eb] + Vector3.Distance(nodes[eb], end);
            var last = viaA <= viaB ? ea : eb;
            if (float.IsPositiveInfinity(dist[last])) return new List<Vector3> { start }; // unreachable

            var path = new List<Vector3> { end };
            for (var n = last; n >= 0; n = prev[n]) path.Add(nodes[n]);
            path.Add(start);
            path.Reverse();
            return path;
        }

        static Vector3 ClosestOnSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
            return a + ab * t;
        }
    }
}
