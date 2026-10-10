using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastFare.Driving
{
    public enum RoadKind { Street, Highway }

    /// <summary>One straight piece of road between two intersections (or bend points).</summary>
    [Serializable]
    public struct RoadSegment
    {
        public int A;
        public int B;
        public RoadKind Kind;
        public float Width;
        public string Name;

        public RoadSegment(int a, int b, RoadKind kind, float width, string name)
        {
            A = a;
            B = b;
            Kind = kind;
            Width = width;
            Name = name;
        }
    }

    /// <summary>A named place shown on the GPS map (neighbourhoods, downtown).</summary>
    [Serializable]
    public struct MapLabel
    {
        public string Text;
        public Vector3 Position;

        public MapLabel(string text, Vector3 position)
        {
            Text = text;
            Position = position;
        }
    }

    /// <summary>
    /// Scene handle for the drivable roads: intersections, named road segments and map labels, serialized
    /// so the scene is the source of truth. Builds the <see cref="RoadNetwork"/> the GPS routes on, and
    /// answers "is this point on a road?" for the car's off-road handling.
    /// </summary>
    public class RoadMap : MonoBehaviour
    {
        [SerializeField] Vector3[] nodes = Array.Empty<Vector3>();
        [Tooltip("Turning-circle radius at dead ends (cul-de-sacs); 0 for ordinary nodes.")]
        [SerializeField] float[] nodeRadius = Array.Empty<float>();
        [SerializeField] RoadSegment[] roads = Array.Empty<RoadSegment>();
        [SerializeField] MapLabel[] labels = Array.Empty<MapLabel>();

        RoadNetwork network;

        public int NodeCount => nodes.Length;
        public int RoadCount => roads.Length;
        public Vector3 Node(int index) => nodes[index];
        public float NodeRadius(int index) => index < nodeRadius.Length ? nodeRadius[index] : 0f;
        public RoadSegment Road(int index) => roads[index];
        public IReadOnlyList<MapLabel> Labels => labels;

        public RoadNetwork Network
        {
            get
            {
                if (network != null) return network;
                var edges = new (int, int)[roads.Length];
                for (var i = 0; i < roads.Length; i++) edges[i] = (roads[i].A, roads[i].B);
                return network = new RoadNetwork(nodes, edges);
            }
        }

        /// <summary>Replaces the whole map (used by the editor world builder).</summary>
        public void SetData(Vector3[] nodePositions, float[] radii, RoadSegment[] segments, MapLabel[] mapLabels)
        {
            nodes = nodePositions;
            nodeRadius = radii;
            roads = segments;
            labels = mapLabels;
            network = null;
        }

        /// <summary>True when <paramref name="point"/> is on the tarmac (within <paramref name="margin"/> metres of it).</summary>
        public bool IsOnRoad(Vector3 point, float margin = 0.5f)
        {
            point.y = 0f;
            var closest = Network.ClosestPoint(point, out var edge);
            if (Vector3.Distance(closest, point) <= roads[edge].Width * 0.5f + margin) return true;

            // Cul-de-sac bulbs are wider than their street.
            var (a, b) = (roads[edge].A, roads[edge].B);
            return Vector3.Distance(nodes[a], point) <= NodeRadius(a) + margin
                   || Vector3.Distance(nodes[b], point) <= NodeRadius(b) + margin;
        }

        /// <summary>Name of the road nearest to <paramref name="point"/>.</summary>
        public string RoadNameAt(Vector3 point)
        {
            if (roads.Length == 0) return "";
            Network.ClosestPoint(point, out var edge);
            return roads[edge].Name;
        }

        void OnValidate() => network = null;

        void OnDrawGizmosSelected()
        {
            if (roads.Length == 0) return;
            foreach (var r in roads)
            {
                Gizmos.color = r.Kind == RoadKind.Highway ? new Color(1f, 0.5f, 0.2f) : new Color(1f, 0.85f, 0.3f);
                Gizmos.DrawLine(nodes[r.A] + Vector3.up * 0.3f, nodes[r.B] + Vector3.up * 0.3f);
            }
        }
    }
}
