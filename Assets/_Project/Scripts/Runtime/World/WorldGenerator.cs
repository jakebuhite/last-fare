using System;
using System.Collections.Generic;
using LastFare.Driving;
using UnityEngine;

namespace LastFare.World
{
    /// <summary>
    /// Lays out the night-shift world: a downtown grid where fares start, highways out of town, and
    /// suburban neighbourhoods of cul-de-sacs where passengers live. Deterministic for a given seed.
    /// </summary>
    public static class WorldGenerator
    {
        // Downtown
        const int GridSize = 6;                     // intersections per side: 5 x 5 blocks
        public const float Block = 60f;
        public const float DowntownStreetWidth = 10f;
        public const float DowntownLane = 2.5f;

        // Highways
        public const float HighwayWidth = 16f;
        const float HighwaySpacing = 30f;
        const float HighwayLampSpacing = 70f;

        // Neighbourhoods
        public const float StreetWidth = 8f;
        public const float StreetLane = 2f;
        public const float FrontYard = 7f;          // street edge to front wall
        public const float BulbRadius = 13f;        // cul-de-sac turning circle
        const float LotSpacing = 24f;
        const float StreetSpacing = 20f;
        const float JunctionSpacing = 90f;
        const float JunctionClearance = 34f;        // no houses on the main drive this close to a side street
        const float StreetLampSpacing = 50f;

        public const float WorldExtent = 1600f;

        static readonly string[] Avenues = { "Grant Avenue", "Mercer Avenue", "Halloway Avenue", "Pike Avenue", "Dorsey Avenue", "Vance Avenue" };
        static readonly string[] CrossStreets = { "Canal Street", "Market Street", "Church Street", "Mill Street", "Water Street", "Hill Street" };

        public static readonly NeighborhoodStyle HarrowHeights = new()
        {
            Name = "Harrow Heights",
            MainStreet = "Harrow Heights Drive",
            Streets = new[] { "Juniper Court", "Ashby Lane", "Corwin Court", "Fallow Way", "Pinecrest Court", "Hale Lane" },
            Branches = 6,
            Bend = 10f,
            PorchLightChance = 0.55f,
            LitWindowChance = 0.45f,
            YardTreeChance = 0.45f,
            Trees = new[] { TreeKind.Round, TreeKind.Round, TreeKind.Pine },
        };

        public static readonly NeighborhoodStyle WrenHollow = new()
        {
            Name = "Wren Hollow",
            MainStreet = "Wren Hollow Road",
            Streets = new[] { "Thistle Lane", "Bramble Court", "Sorrel Way", "Hollow Court", "Fenwick Lane" },
            Branches = 5,
            BranchMin = 120f,
            BranchMax = 180f,
            Bend = -14f,
            PorchLightChance = 0.25f,
            LitWindowChance = 0.3f,
            YardTreeChance = 0.95f,
            Trees = new[] { TreeKind.Pine, TreeKind.Pine, TreeKind.Round },
        };

        public static readonly NeighborhoodStyle AldousPark = new()
        {
            Name = "Aldous Park",
            MainStreet = "Aldous Park Drive",
            Streets = new[] { "Aldous Street", "Linden Court", "Marrow Lane", "Pell Court", "Ivers Way", "Gable Court" },
            Branches = 6,
            Bend = 16f,
            LongStreet = "Aldous Street",
            PorchLightChance = 0.2f,
            LitWindowChance = 0.2f,
            YardTreeChance = 0.7f,
            Trees = new[] { TreeKind.Dead, TreeKind.Dead, TreeKind.Round },
        };

        struct HighwaySpec
        {
            public string Name;
            public int GridX, GridZ;
            public Vector3[] Controls;  // after the downtown node
            public NeighborhoodStyle Destination;
            public bool AbandonedCar;
        }

        static readonly HighwaySpec[] HighwaySpecs =
        {
            new()
            {
                Name = "Route 9", GridX = 3, GridZ = 5, Destination = HarrowHeights,
                Controls = new[] { new Vector3(30f, 0f, 280f), new Vector3(95f, 0f, 430f), new Vector3(70f, 0f, 610f), new Vector3(130f, 0f, 780f), new Vector3(150f, 0f, 900f) },
            },
            new()
            {
                Name = "Route 40", GridX = 5, GridZ = 2, Destination = WrenHollow, AbandonedCar = true,
                Controls = new[] { new Vector3(290f, 0f, -30f), new Vector3(460f, 0f, 25f), new Vector3(640f, 0f, -5f), new Vector3(820f, 0f, 55f), new Vector3(960f, 0f, 70f) },
            },
            new()
            {
                Name = "Route 12", GridX = 0, GridZ = 0, Destination = AldousPark, AbandonedCar = true,
                Controls = new[] { new Vector3(-260f, 0f, -240f), new Vector3(-400f, 0f, -320f), new Vector3(-530f, 0f, -480f), new Vector3(-610f, 0f, -660f), new Vector3(-720f, 0f, -800f) },
            },
        };

        public static WorldPlan Generate(int seed = 1234)
        {
            var plan = new WorldPlan { WorldExtent = WorldExtent };
            var rng = new System.Random(seed);

            var grid = BuildDowntown(plan, rng);
            foreach (var spec in HighwaySpecs) BuildHighway(plan, spec, grid, rng);
            PlaceTrees(plan, rng);
            return plan;
        }

        // ------------------------------------------------------------- Downtown

        static int[,] BuildDowntown(WorldPlan plan, System.Random rng)
        {
            var half = (GridSize - 1) * Block * 0.5f;
            var origin = new Vector3(-half, 0f, -half);
            var grid = new int[GridSize, GridSize];

            for (var z = 0; z < GridSize; z++)
            {
                for (var x = 0; x < GridSize; x++)
                {
                    grid[x, z] = plan.AddNode(origin + new Vector3(x * Block, 0f, z * Block));
                    plan.DowntownNodes.Add(grid[x, z]);
                }
            }

            for (var z = 0; z < GridSize; z++)
            {
                for (var x = 0; x < GridSize; x++)
                {
                    if (x + 1 < GridSize) plan.AddRoad(grid[x, z], grid[x + 1, z], RoadKind.Street, DowntownStreetWidth, CrossStreets[z]);
                    if (z + 1 < GridSize) plan.AddRoad(grid[x, z], grid[x, z + 1], RoadKind.Street, DowntownStreetWidth, Avenues[x]);
                }
            }

            plan.DowntownMin = origin;
            plan.DowntownMax = origin + new Vector3((GridSize - 1) * Block, 0f, (GridSize - 1) * Block);

            var cemetery = new Vector2Int(1, 3);
            var park = new Vector2Int(3, 1);
            var parking = new Vector2Int(2, 0);
            for (var bz = 0; bz < GridSize - 1; bz++)
            {
                for (var bx = 0; bx < GridSize - 1; bx++)
                {
                    var cell = new Vector2Int(bx, bz);
                    plan.Blocks.Add(new BlockPlan
                    {
                        Center = origin + new Vector3((bx + 0.5f) * Block, 0f, (bz + 0.5f) * Block),
                        Size = Block - DowntownStreetWidth,
                        Use = cell == cemetery ? BlockUse.Cemetery : cell == park ? BlockUse.Park : cell == parking ? BlockUse.Parking : BlockUse.Buildings,
                    });
                }
            }

            // A lamp on the north-east corner of every intersection, arm reaching over the crossing.
            var corner = DowntownStreetWidth * 0.5f + 1f;
            for (var i = 0; i < plan.Nodes.Count; i++)
            {
                var node = plan.Nodes[i];
                var basePoint = node + new Vector3(corner, 0f, corner);
                plan.Lamps.Add(new LampPlan { Position = basePoint, Rotation = Quaternion.LookRotation(node - basePoint), Kind = LampKind.Street });
            }

            // The fare: Mercy Hill Cemetery's north gate opens onto the street along the top of its block.
            var gateX = origin.x + (cemetery.x + 0.5f) * Block;
            var northStreetZ = origin.z + (cemetery.y + 1) * Block;
            var kerb = DowntownStreetWidth * 0.5f;
            plan.CemeteryGate = new Vector3(gateX, 0f, northStreetZ - kerb - 1f);
            plan.PickupPoint = new Vector3(gateX, 0f, northStreetZ - DowntownLane);
            plan.PickupStandPoint = new Vector3(gateX - 1.2f, 0f, northStreetZ - kerb - 1.5f);
            plan.PickupLookPoint = new Vector3(gateX - 1.2f, 0f, northStreetZ);

            // The driver waits on Canal Street outside the rideshare lot, westbound (towards the cemetery).
            var lot = origin + new Vector3((parking.x + 0.5f) * Block, 0f, (parking.y + 0.5f) * Block);
            plan.StartPosition = new Vector3(lot.x + 12f, 0f, origin.z + DowntownLane);
            plan.StartRotation = Quaternion.LookRotation(Vector3.left);

            // A few cars left in the lot.
            for (var i = 0; i < 8; i++)
            {
                if (rng.NextDouble() < 0.45) continue;
                var row = i < 4 ? -1 : 1;
                var stall = new Vector3(-18f + (i % 4) * 12f, 0f, row * 10f);
                plan.Props.Add(new PropPlan
                {
                    Position = lot + stall,
                    Rotation = Quaternion.LookRotation(row < 0 ? Vector3.forward : Vector3.back),
                    Kind = PropKind.ParkedCar,
                });
            }

            plan.Labels.Add(new MapLabel("Downtown", Vector3.zero));
            return grid;
        }

        // ------------------------------------------------------------- Highways

        static void BuildHighway(WorldPlan plan, HighwaySpec spec, int[,] grid, System.Random rng)
        {
            var startNode = grid[spec.GridX, spec.GridZ];
            var controls = new List<Vector3> { plan.Nodes[startNode] };
            controls.AddRange(spec.Controls);
            var points = PathSampler.SmoothResample(controls, HighwaySpacing);

            var highway = new HighwayPlan { Name = spec.Name, Destination = spec.Destination.Name };
            highway.Nodes.Add(startNode);
            highway.Nodes.AddRange(plan.AddRoadPath(startNode, points, RoadKind.Highway, HighwayWidth, spec.Name));
            plan.Highways.Add(highway);

            var length = PathSampler.Length(points);
            var edge = HighwayWidth * 0.5f;

            // Tall lamps, alternating sides.
            var side = 1f;
            for (var s = 40f; s < length - 30f; s += HighwayLampSpacing, side = -side)
            {
                var p = PathSampler.PointAt(points, s, out var t);
                var r = PathSampler.RightOf(t) * side;
                plan.Lamps.Add(new LampPlan { Position = p + r * (edge + 1.6f), Rotation = Quaternion.LookRotation(-r), Kind = LampKind.Highway });
            }

            // Green signs: one leaving town, one coming back.
            var km = $"{length / 1000f:0.0} km";
            {
                var p = PathSampler.PointAt(points, 60f, out var t);
                plan.Signs.Add(new SignPlan
                {
                    Position = p + PathSampler.RightOf(t) * (edge + 3f),
                    Rotation = Quaternion.LookRotation(t),
                    Text = $"{spec.Name.ToUpperInvariant()}\n{spec.Destination.Name}  {km}",
                    Kind = SignKind.Highway,
                });
            }
            {
                var p = PathSampler.PointAt(points, length - 60f, out var t);
                plan.Signs.Add(new SignPlan
                {
                    Position = p - PathSampler.RightOf(t) * (edge + 3f),
                    Rotation = Quaternion.LookRotation(-t),
                    Text = $"{spec.Name.ToUpperInvariant()}\nDowntown  {km}",
                    Kind = SignKind.Highway,
                });
            }

            if (spec.AbandonedCar)
            {
                var s = length * (0.35f + (float)rng.NextDouble() * 0.3f);
                var p = PathSampler.PointAt(points, s, out var t);
                plan.Props.Add(new PropPlan
                {
                    Position = p + PathSampler.RightOf(t) * (edge - 1f),
                    Rotation = Quaternion.LookRotation(t) * Quaternion.Euler(0f, 6f, 0f),
                    Kind = PropKind.AbandonedCar,
                });
            }

            var end = highway.Nodes[highway.Nodes.Count - 1];
            var direction = points[points.Count - 1] - points[points.Count - 2];
            BuildNeighborhood(plan, end, direction, spec.Destination, rng);
        }

        // ------------------------------------------------------- Neighbourhoods

        static void BuildNeighborhood(WorldPlan plan, int entrance, Vector3 direction, NeighborhoodStyle style, System.Random rng)
        {
            var index = plan.Neighborhoods.Count;
            var hood = new NeighborhoodPlan { Style = style, Entrance = plan.Nodes[entrance] };
            plan.Neighborhoods.Add(hood);

            var d = PathSampler.Flat(direction).normalized;
            var right = PathSampler.RightOf(d);
            var origin = plan.Nodes[entrance];

            // Main drive: straight on from the highway, side streets at regular junctions, a cul-de-sac at the end.
            var junctionCount = (style.Branches + 1) / 2;
            var keys = new List<float> { 0f };
            for (var j = 0; j < junctionCount; j++) keys.Add(80f + j * JunctionSpacing);
            var mainLength = keys[keys.Count - 1] + 80f;
            keys.Add(mainLength);

            var mainPoints = new List<Vector3> { origin };
            var junctionNodes = new List<int>();
            var previous = entrance;
            hood.Nodes.Add(entrance);
            for (var k = 1; k < keys.Count; k++)
            {
                var span = keys[k] - keys[k - 1];
                var pieces = Mathf.CeilToInt(span / 25f);
                for (var p = 1; p <= pieces; p++)
                {
                    var last = k == keys.Count - 1 && p == pieces;
                    var pos = origin + d * (keys[k - 1] + span * p / pieces);
                    var id = plan.AddNode(pos, last ? BulbRadius : 0f);
                    plan.AddRoad(previous, id, RoadKind.Street, StreetWidth, style.MainStreet);
                    previous = id;
                    hood.Nodes.Add(id);
                    mainPoints.Add(pos);
                    if (p == pieces && !last) junctionNodes.Add(id);
                }
            }

            var junctionS = keys.GetRange(1, junctionCount);
            var count = PlaceHouseRow(plan, mainPoints, style.MainStreet, index, 40f, mainLength - 18f,
                s => junctionS.Exists(j => Mathf.Abs(s - j) < JunctionClearance));
            PlaceCulDeSacHouses(plan, mainPoints, style.MainStreet, index, count);
            PlaceStreetLamps(plan, mainPoints, 20f);

            plan.Signs.Add(new SignPlan
            {
                Position = origin + d * 14f + right * (StreetWidth * 0.5f + 4f),
                Rotation = Quaternion.LookRotation(d),
                Text = style.Name,
                Kind = SignKind.Neighborhood,
            });

            // Side streets: both sides of each junction, curving gently, each ending in a cul-de-sac.
            var branch = 0;
            for (var j = 0; j < junctionNodes.Count; j++)
            {
                foreach (var side in new[] { -1f, 1f })
                {
                    if (branch >= style.Branches) break;

                    var name = style.Streets[branch % style.Streets.Length];
                    var length = name == style.LongStreet
                        ? style.LongStreetLength
                        : Mathf.Lerp(style.BranchMin, style.BranchMax, (float)rng.NextDouble());
                    var junction = plan.Nodes[junctionNodes[j]];
                    var points = PathSampler.Arc(junction, right * side, length, style.Bend, StreetSpacing);

                    var ids = plan.AddRoadPath(junctionNodes[j], points, RoadKind.Street, StreetWidth, name);
                    plan.NodeRadius[ids[ids.Count - 1]] = BulbRadius;
                    hood.Nodes.AddRange(ids);

                    var rows = PlaceHouseRow(plan, points, name, index, 24f, length - 18f, null);
                    PlaceCulDeSacHouses(plan, points, name, index, rows);
                    PlaceStreetLamps(plan, points, 25f);

                    plan.Signs.Add(new SignPlan
                    {
                        Position = junction + right * side * (StreetWidth * 0.5f + 2f) - d * (StreetWidth * 0.5f + 2f),
                        Rotation = Quaternion.LookRotation(d),
                        Text = name,
                        Kind = SignKind.Street,
                    });

                    branch++;
                }
            }

            var centre = Vector3.zero;
            var houses = 0;
            foreach (var h in plan.Houses)
            {
                if (h.Neighborhood != index) continue;
                centre += h.Position;
                houses++;
            }

            hood.Center = houses > 0 ? centre / houses : origin;
            plan.Labels.Add(new MapLabel(style.Name, hood.Center));
        }

        /// <summary>Houses facing the street on both sides: even numbers on the right, odd on the left. Returns lots per side.</summary>
        static int PlaceHouseRow(WorldPlan plan, List<Vector3> street, string name, int neighborhood, float from, float to,
            Predicate<float> skip)
        {
            var i = 0;
            for (var s = from; s <= to; s += LotSpacing, i++)
            {
                if (skip != null && skip(s)) continue;
                var curb = PathSampler.PointAt(street, s, out var t);
                var r = PathSampler.RightOf(t);
                AddHouse(plan, curb, r, StreetWidth * 0.5f + FrontYard, name, 100 + 2 * i, neighborhood);
                AddHouse(plan, curb, -r, StreetWidth * 0.5f + FrontYard, name, 101 + 2 * i, neighborhood);
            }

            return i;
        }

        /// <summary>Three houses round the turning circle at the end of a street, numbered on from the rows.</summary>
        static void PlaceCulDeSacHouses(WorldPlan plan, List<Vector3> street, string name, int neighborhood, int rows)
        {
            var end = street[street.Count - 1];
            var t = PathSampler.Flat(end - street[street.Count - 2]).normalized;
            var lots = new[] { (70f, 100 + 2 * rows), (0f, 102 + 2 * rows), (-70f, 101 + 2 * rows) };
            foreach (var (angle, number) in lots)
            {
                var outward = Quaternion.Euler(0f, angle, 0f) * t;
                var curb = end + outward * (BulbRadius * 0.5f);
                AddHouse(plan, end, outward, BulbRadius + FrontYard - 1f, name, number, neighborhood, curb);
            }
        }

        static void AddHouse(WorldPlan plan, Vector3 from, Vector3 outward, float distance, string street, int number,
            int neighborhood, Vector3? curb = null)
        {
            plan.Houses.Add(new HouseLot
            {
                Position = from + outward * distance,
                Rotation = Quaternion.LookRotation(-outward),
                Curb = curb ?? from,
                Street = street,
                Number = number,
                Neighborhood = neighborhood,
            });
        }

        static void PlaceStreetLamps(WorldPlan plan, List<Vector3> street, float firstAt)
        {
            var length = PathSampler.Length(street);
            var side = 1f;
            for (var s = firstAt; s < length - 10f; s += StreetLampSpacing, side = -side)
            {
                var p = PathSampler.PointAt(street, s, out var t);
                var r = PathSampler.RightOf(t) * side;
                plan.Lamps.Add(new LampPlan { Position = p + r * (StreetWidth * 0.5f + 0.4f), Rotation = Quaternion.LookRotation(-r), Kind = LampKind.Street });
            }
        }

        // ---------------------------------------------------------------- Trees

        static void PlaceTrees(WorldPlan plan, System.Random rng)
        {
            var candidates = new List<TreePlan>();

            TreeKind Wild() => rng.NextDouble() switch
            {
                < 0.5 => TreeKind.Pine,
                < 0.8 => TreeKind.Round,
                _ => TreeKind.Dead,
            };

            // Back gardens.
            foreach (var house in plan.Houses)
            {
                var style = plan.Neighborhoods[house.Neighborhood].Style;
                if (rng.NextDouble() > style.YardTreeChance) continue;
                var side = Vector3.Cross(Vector3.up, house.Front);
                candidates.Add(Tree(house.Position - house.Front * (16f + (float)rng.NextDouble() * 8f) + side * (((float)rng.NextDouble() - 0.5f) * 10f),
                    style.Trees[rng.Next(style.Trees.Length)]));
            }

            // Woods along the highways.
            foreach (var highway in plan.Highways)
            {
                for (var i = 1; i < highway.Nodes.Count; i++)
                {
                    var a = plan.Nodes[highway.Nodes[i - 1]];
                    var b = plan.Nodes[highway.Nodes[i]];
                    var right = PathSampler.RightOf(b - a);
                    for (var k = 0; k < 4; k++)
                    {
                        if (rng.NextDouble() > 0.6) continue;
                        var along = Vector3.Lerp(a, b, (float)rng.NextDouble());
                        var side = rng.NextDouble() < 0.5 ? -1f : 1f;
                        candidates.Add(Tree(along + right * side * (HighwayWidth * 0.5f + 6f + (float)rng.NextDouble() * 40f), Wild()));
                    }
                }
            }

            // Open country.
            for (var i = 0; i < 320; i++)
            {
                var p = new Vector3(((float)rng.NextDouble() * 2f - 1f) * (WorldExtent - 100f), 0f,
                    ((float)rng.NextDouble() * 2f - 1f) * (WorldExtent - 100f));
                candidates.Add(Tree(p, Wild()));
            }

            var kept = new List<TreePlan>();
            foreach (var tree in candidates)
                if (IsClear(plan, tree.Position, kept))
                    kept.Add(tree);

            // Downtown greenery is placed by hand so it can sit inside the blocks.
            foreach (var block in plan.Blocks)
            {
                if (block.Use == BlockUse.Park)
                {
                    for (var i = 0; i < 10; i++)
                    {
                        var offset = new Vector3(((float)rng.NextDouble() - 0.5f) * (block.Size - 10f), 0f, ((float)rng.NextDouble() - 0.5f) * (block.Size - 10f));
                        kept.Add(Tree(block.Center + offset, rng.NextDouble() < 0.7 ? TreeKind.Round : TreeKind.Pine));
                    }
                }
                else if (block.Use == BlockUse.Cemetery)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        var offset = new Vector3(((float)rng.NextDouble() - 0.5f) * (block.Size - 12f), 0f, ((float)rng.NextDouble() - 0.4f) * (block.Size - 16f));
                        kept.Add(Tree(block.Center + offset, TreeKind.Dead));
                    }
                }
            }

            plan.Trees.AddRange(kept);

            TreePlan Tree(Vector3 position, TreeKind kind) => new()
            {
                Position = position,
                Kind = kind,
                Scale = 0.8f + (float)rng.NextDouble() * 0.5f,
                Yaw = (float)rng.NextDouble() * 360f,
            };
        }

        static bool IsClear(WorldPlan plan, Vector3 p, List<TreePlan> trees)
        {
            if (Mathf.Abs(p.x) > WorldExtent - 20f || Mathf.Abs(p.z) > WorldExtent - 20f) return false;
            if (p.x > plan.DowntownMin.x - 12f && p.x < plan.DowntownMax.x + 12f &&
                p.z > plan.DowntownMin.z - 12f && p.z < plan.DowntownMax.z + 12f) return false;
            if (DistanceToTarmac(plan, p) < 4f) return false;

            foreach (var house in plan.Houses)
                if ((house.Position - house.Front * 6f - p).sqrMagnitude < 12f * 12f) return false;
            foreach (var tree in trees)
                if ((tree.Position - p).sqrMagnitude < 3.5f * 3.5f) return false;

            return true;
        }

        /// <summary>Metres from <paramref name="p"/> to the nearest road surface (negative when on it).</summary>
        public static float DistanceToTarmac(WorldPlan plan, Vector3 p)
        {
            var closest = plan.Network.ClosestPoint(p, out var edge);
            var road = plan.Roads[edge];
            var distance = Vector3.Distance(closest, p) - road.Width * 0.5f;
            distance = Mathf.Min(distance, Vector3.Distance(plan.Nodes[road.A], p) - plan.NodeRadius[road.A]);
            distance = Mathf.Min(distance, Vector3.Distance(plan.Nodes[road.B], p) - plan.NodeRadius[road.B]);
            return distance;
        }
    }
}
