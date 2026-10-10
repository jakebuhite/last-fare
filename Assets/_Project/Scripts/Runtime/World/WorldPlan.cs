using System.Collections.Generic;
using LastFare.Driving;
using UnityEngine;

namespace LastFare.World
{
    public enum BlockUse { Buildings, Cemetery, Park, Parking }
    public enum TreeKind { Pine, Round, Dead }
    public enum LampKind { Street, Highway }
    public enum SignKind { Highway, Street, Neighborhood }
    public enum PropKind { AbandonedCar, ParkedCar }

    /// <summary>A downtown block between four streets.</summary>
    public struct BlockPlan
    {
        public Vector3 Center;
        public float Size;    // inner size, kerb to kerb
        public BlockUse Use;
    }

    /// <summary>A house plot. The house's front wall sits at <see cref="Position"/>; its +Z faces the street.</summary>
    public struct HouseLot
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Curb;          // point on the street centreline in front of the house
        public string Street;
        public int Number;
        public int Neighborhood;

        public string Address => $"{Number} {Street}";
        public Vector3 Front => Rotation * Vector3.forward;
    }

    public struct TreePlan
    {
        public Vector3 Position;
        public TreeKind Kind;
        public float Scale;
        public float Yaw;
    }

    /// <summary>A lamp post; its arm reaches along +Z, out over the road.</summary>
    public struct LampPlan
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public LampKind Kind;
    }

    /// <summary>A sign, readable by someone looking along its +Z.</summary>
    public struct SignPlan
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public string Text;
        public SignKind Kind;
    }

    public struct PropPlan
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public PropKind Kind;
    }

    /// <summary>How a neighbourhood looks and feels.</summary>
    public class NeighborhoodStyle
    {
        public string Name;
        public string MainStreet;
        public string[] Streets;
        public int Branches = 6;
        public float BranchMin = 130f;
        public float BranchMax = 200f;
        public float Bend = 12f;                // degrees each side street curves over its length
        public string LongStreet;               // optional street forced to LongStreetLength (e.g. a known address)
        public float LongStreetLength = 230f;
        public float PorchLightChance = 0.35f;
        public float LitWindowChance = 0.35f;
        public float YardTreeChance = 0.6f;
        public TreeKind[] Trees = { TreeKind.Round, TreeKind.Pine };
    }

    public class NeighborhoodPlan
    {
        public NeighborhoodStyle Style;
        public Vector3 Entrance;
        public Vector3 Center;
        public readonly List<int> Nodes = new();
    }

    public class HighwayPlan
    {
        public string Name;
        public string Destination;
        public readonly List<int> Nodes = new();
    }

    /// <summary>
    /// Everything the world builder needs to lay out the map: roads, downtown blocks, houses with addresses,
    /// trees, lamps, signs, props and the fare's stops. Produced by <see cref="WorldGenerator"/>; plain C# so the
    /// layout can be unit tested.
    /// </summary>
    public class WorldPlan
    {
        public readonly List<Vector3> Nodes = new();
        public readonly List<float> NodeRadius = new();
        public readonly List<RoadSegment> Roads = new();
        public readonly HashSet<int> DowntownNodes = new();
        public readonly List<BlockPlan> Blocks = new();
        public readonly List<HighwayPlan> Highways = new();
        public readonly List<NeighborhoodPlan> Neighborhoods = new();
        public readonly List<HouseLot> Houses = new();
        public readonly List<TreePlan> Trees = new();
        public readonly List<LampPlan> Lamps = new();
        public readonly List<SignPlan> Signs = new();
        public readonly List<PropPlan> Props = new();
        public readonly List<MapLabel> Labels = new();

        public Vector3 DowntownMin;
        public Vector3 DowntownMax;
        public float WorldExtent;

        public Vector3 StartPosition;
        public Quaternion StartRotation;
        public Vector3 PickupPoint;
        public Vector3 PickupStandPoint;
        public Vector3 PickupLookPoint;
        public Vector3 CemeteryGate;

        RoadNetwork network;

        public int AddNode(Vector3 position, float radius = 0f)
        {
            Nodes.Add(position);
            NodeRadius.Add(radius);
            network = null;
            return Nodes.Count - 1;
        }

        public void AddRoad(int a, int b, RoadKind kind, float width, string name)
        {
            Roads.Add(new RoadSegment(a, b, kind, width, name));
            network = null;
        }

        /// <summary>Adds a road through <paramref name="points"/>, starting at an existing node. Returns the new node ids (excluding the start).</summary>
        public List<int> AddRoadPath(int startNode, IReadOnlyList<Vector3> points, RoadKind kind, float width, string name)
        {
            var ids = new List<int>();
            var previous = startNode;
            for (var i = 1; i < points.Count; i++)
            {
                var id = AddNode(points[i]);
                AddRoad(previous, id, kind, width, name);
                ids.Add(id);
                previous = id;
            }

            return ids;
        }

        public RoadNetwork Network
        {
            get
            {
                if (network != null) return network;
                var edges = new (int, int)[Roads.Count];
                for (var i = 0; i < Roads.Count; i++) edges[i] = (Roads[i].A, Roads[i].B);
                return network = new RoadNetwork(Nodes.ToArray(), edges);
            }
        }

        public bool TryFindHouse(string address, out HouseLot house)
        {
            foreach (var h in Houses)
            {
                if (h.Address != address) continue;
                house = h;
                return true;
            }

            house = default;
            return false;
        }

        /// <summary>Where a car should stop for a house: the lane on the house's side of the street.</summary>
        public static Vector3 CurbsideStop(HouseLot house, float laneOffset) => house.Curb - house.Front * laneOffset;
    }
}
