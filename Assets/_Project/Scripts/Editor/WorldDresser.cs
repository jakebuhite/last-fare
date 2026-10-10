using System.Collections.Generic;
using LastFare.Driving;
using LastFare.World;
using UnityEditor;
using UnityEngine;

namespace LastFare.Editor
{
    /// <summary>
    /// Dresses the scene from a <see cref="WorldPlan"/>: ground, road surfaces (merged meshes), downtown blocks
    /// with the cemetery, park and rideshare lot, highways with rails and signs, neighbourhood houses, lamps,
    /// trees and props. Repeated things are prefab instances; one-off things are greybox primitives.
    /// </summary>
    public class WorldDresser
    {
        static readonly string[] NeonWords = { "OPEN", "BAR", "MOTEL", "24 HR", "LIQUOR", "DINER", "PAWN", "PHARMACY", "VACANCY", "CHECKS CASHED", "LAUNDRY", "NO VACANCY" };
        static readonly Color[] NeonColors = { new(1f, 0.25f, 0.35f), new(0.3f, 0.7f, 1f), new(0.4f, 1f, 0.5f), new(1f, 0.6f, 0.2f), new(0.9f, 0.3f, 1f) };

        readonly WorldPlan plan;
        readonly WorldPrefabs prefabs;
        readonly System.Random rng;
        readonly string highlightAddress;
        readonly Font font;
        readonly int[] degree;
        readonly bool[] touchesHighway;

        Transform root;
        Material asphalt, highwayAsphalt, lotAsphalt, yellow, white, pavement, grass, ground, rail, stone, iron, warmWindow, glass, signGreen, wood;
        Material[] buildingShades;

        public WorldDresser(WorldPlan plan, WorldPrefabs prefabs, System.Random rng, string highlightAddress)
        {
            this.plan = plan;
            this.prefabs = prefabs;
            this.rng = rng;
            this.highlightAddress = highlightAddress;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            degree = new int[plan.Nodes.Count];
            touchesHighway = new bool[plan.Nodes.Count];
            foreach (var r in plan.Roads)
            {
                degree[r.A]++;
                degree[r.B]++;
                if (r.Kind != RoadKind.Highway) continue;
                touchesHighway[r.A] = true;
                touchesHighway[r.B] = true;
            }
        }

        public Transform Build()
        {
            root = new GameObject("Environment").transform;
            CreateMaterials();

            BuildGround();
            var downtown = Group(root, "Downtown");
            BuildDowntownStreets(downtown);
            BuildBlocks(downtown);

            var highways = Group(root, "Highways");
            foreach (var highway in plan.Highways) BuildHighway(highways, highway);

            var hoods = Group(root, "Neighborhoods");
            for (var i = 0; i < plan.Neighborhoods.Count; i++) BuildNeighborhood(hoods, plan.Neighborhoods[i], i);

            PlaceLamps();
            PlaceTrees();
            PlaceSigns();
            PlaceProps();
            BuildBoundary();

            MarkStatic(root);
            return root;
        }

        void CreateMaterials()
        {
            asphalt = Prim.Lit(new Color(0.07f, 0.07f, 0.08f), 0.45f);
            highwayAsphalt = Prim.Lit(new Color(0.08f, 0.08f, 0.085f), 0.5f);
            lotAsphalt = Prim.Lit(new Color(0.06f, 0.06f, 0.065f), 0.4f);
            yellow = Prim.Lit(new Color(0.75f, 0.62f, 0.2f), 0.1f);
            white = Prim.Lit(new Color(0.7f, 0.7f, 0.68f), 0.1f);
            pavement = Prim.Lit(new Color(0.12f, 0.12f, 0.125f), 0.1f);
            grass = Prim.Lit(new Color(0.04f, 0.06f, 0.04f), 0.02f);
            ground = Prim.Lit(new Color(0.045f, 0.05f, 0.04f), 0.02f);
            rail = Prim.Lit(new Color(0.3f, 0.3f, 0.32f), 0.6f);
            stone = Prim.Lit(new Color(0.16f, 0.17f, 0.18f), 0.1f);
            iron = Prim.Lit(new Color(0.05f, 0.05f, 0.06f), 0.3f);
            warmWindow = Prim.Unlit(new Color(1f, 0.72f, 0.35f) * 0.85f);
            glass = Prim.Lit(new Color(0.03f, 0.04f, 0.06f), 0.85f);
            signGreen = Prim.Lit(new Color(0.04f, 0.22f, 0.1f), 0.3f);
            wood = Prim.Lit(new Color(0.12f, 0.08f, 0.05f), 0.1f);
            buildingShades = new Material[4];
            for (var i = 0; i < buildingShades.Length; i++)
            {
                var s = 0.05f + i * 0.02f;
                buildingShades[i] = Prim.Lit(new Color(s, s * 1.05f, s * 1.2f), 0.05f);
            }
        }

        // ---------------------------------------------------------------- Ground

        void BuildGround()
        {
            var size = plan.WorldExtent * 2f + 200f;
            Prim.Box(root, "Ground", new Vector3(0f, -0.1f, 0f), new Vector3(size, 0.2f, size), ground);
        }

        void BuildBoundary()
        {
            // Invisible walls at the edge of the world.
            var walls = Group(root, "BoundaryWalls");
            var e = plan.WorldExtent;
            void Wall(string name, Vector3 pos, Vector3 size)
            {
                var go = Child(walls, name);
                go.transform.position = pos + Vector3.up * 5f;
                go.AddComponent<BoxCollider>().size = size;
            }

            Wall("North", new Vector3(0f, 0f, e), new Vector3(e * 2f, 10f, 1f));
            Wall("South", new Vector3(0f, 0f, -e), new Vector3(e * 2f, 10f, 1f));
            Wall("East", new Vector3(e, 0f, 0f), new Vector3(1f, 10f, e * 2f));
            Wall("West", new Vector3(-e, 0f, 0f), new Vector3(1f, 10f, e * 2f));
        }

        // -------------------------------------------------------------- Downtown

        void BuildDowntownStreets(Transform parent)
        {
            var batch = new MeshBatcher();
            var w = WorldGenerator.DowntownStreetWidth;
            var half = w * 0.5f;

            foreach (var r in plan.Roads)
            {
                if (!plan.DowntownNodes.Contains(r.A) || !plan.DowntownNodes.Contains(r.B)) continue;
                var a = plan.Nodes[r.A];
                var b = plan.Nodes[r.B];
                var dir = (b - a).normalized;
                var a2 = a + dir * half;
                var b2 = b - dir * half;

                batch.Strip(a2, b2, 0f, w, 0.1f, 0.01f, asphalt);
                batch.Dashes(a2, b2, 0f, 0.15f, 3f, 8f, 0.025f, yellow, 6f);

                // Zebra crossings at both ends.
                for (var x = -half + 1f; x <= half - 1f; x += 1.4f)
                {
                    batch.Strip(a2 + dir * 0.6f, a2 + dir * 3.4f, x, 0.55f, 0.02f, 0.03f, white);
                    batch.Strip(b2 - dir * 3.4f, b2 - dir * 0.6f, x, 0.55f, 0.02f, 0.03f, white);
                }
            }

            foreach (var n in plan.DowntownNodes)
                batch.Box(plan.Nodes[n] + Vector3.down * 0.04f, Quaternion.identity, new Vector3(w, 0.1f, w), asphalt);

            // Pavement slabs under every block.
            foreach (var block in plan.Blocks)
                batch.Box(block.Center + Vector3.up * 0.06f, Quaternion.identity, new Vector3(block.Size, 0.12f, block.Size), pavement);

            batch.Build(parent, "Streets");
        }

        void BuildBlocks(Transform parent)
        {
            var blocks = Group(parent, "Blocks");
            var windows = new MeshBatcher();
            var neonLeft = 12;

            foreach (var block in plan.Blocks)
            {
                switch (block.Use)
                {
                    case BlockUse.Cemetery:
                        BuildCemetery(parent, block);
                        break;
                    case BlockUse.Park:
                        BuildPark(parent, block);
                        break;
                    case BlockUse.Parking:
                        BuildParkingLot(parent, block);
                        break;
                    default:
                        BuildCityBlock(blocks, block, windows, ref neonLeft);
                        break;
                }
            }

            windows.Build(parent, "Windows");
        }

        void BuildCityBlock(Transform parent, BlockPlan block, MeshBatcher windows, ref int neonLeft)
        {
            var group = Group(parent, "Block");
            group.position = block.Center;
            var lot = (block.Size - 6f) * 0.5f;
            var centrality = 1f - Mathf.Clamp01(block.Center.magnitude / 200f); // taller towards the middle

            for (var lz = 0; lz < 2; lz++)
            {
                for (var lx = 0; lx < 2; lx++)
                {
                    var side = new Vector3(lx * 2 - 1, 0f, lz * 2 - 1);
                    var width = lot - 1f - (float)rng.NextDouble() * 3f;
                    var depth = lot - 1f - (float)rng.NextDouble() * 3f;
                    var height = 8f + (float)rng.NextDouble() * (12f + 30f * centrality);
                    var centre = block.Center + new Vector3(side.x * (lot - width * 0.5f), height * 0.5f, side.z * (lot - depth * 0.5f));

                    var b = Prim.Box(group, "Building", Vector3.zero, Vector3.one, buildingShades[rng.Next(buildingShades.Length)]);
                    b.transform.position = centre;
                    b.transform.localScale = new Vector3(width, height, depth);
                    b.AddComponent<BoxCollider>();

                    // Windows on both street-facing walls: a few lit, more dark.
                    for (var face = 0; face < 2; face++)
                    {
                        var onX = face == 0;
                        var faceCentre = centre + (onX ? new Vector3(side.x * (width * 0.5f + 0.03f), 0f, 0f) : new Vector3(0f, 0f, side.z * (depth * 0.5f + 0.03f)));
                        var along = onX ? Vector3.forward : Vector3.right;
                        var span = (onX ? depth : width) * 0.5f - 1.5f;
                        var floors = Mathf.Max(1, Mathf.FloorToInt((height - 2f) / 3.2f));
                        var count = 3 + rng.Next(6);
                        for (var k = 0; k < count; k++)
                        {
                            var y = centre.y - height * 0.5f + 2.2f + rng.Next(floors) * 3.2f;
                            var pos = new Vector3(faceCentre.x, y, faceCentre.z) + along * (((float)rng.NextDouble() * 2f - 1f) * span);
                            var size = onX ? new Vector3(0.08f, 1.3f, 1.4f) : new Vector3(1.4f, 1.3f, 0.08f);
                            windows.Box(pos, Quaternion.identity, size, rng.NextDouble() < 0.35 ? warmWindow : glass);
                        }

                        if (face == 0 && neonLeft > 0 && rng.NextDouble() < 0.18)
                        {
                            neonLeft--;
                            var facing = onX ? new Vector3(side.x, 0f, 0f) : new Vector3(0f, 0f, side.z);
                            Neon(group, new Vector3(faceCentre.x, 3.6f, faceCentre.z) + facing * 0.15f, facing);
                        }
                    }
                }
            }
        }

        void Neon(Transform parent, Vector3 position, Vector3 facing)
        {
            var color = NeonColors[rng.Next(NeonColors.Length)];
            var sign = Group(parent, "NeonSign");
            sign.position = position;
            sign.rotation = Quaternion.LookRotation(-facing); // readable from the street
            Text(sign, NeonWords[rng.Next(NeonWords.Length)], new Vector3(0f, 0f, -0.02f), 0.7f, color);
            Prim.PointLight(sign, "Glow", new Vector3(0f, 0f, -1.2f), color, 9f, 3f);
        }

        void BuildCemetery(Transform parent, BlockPlan block)
        {
            var c = Group(parent, "MercyHillCemetery");
            c.position = block.Center;
            var half = block.Size * 0.5f - 1f;
            var gateX = plan.CemeteryGate.x - block.Center.x;
            const float gateHalf = 2.5f;

            Prim.Box(c, "Grass", new Vector3(0f, 0.13f, 0f), new Vector3(block.Size - 2f, 0.04f, block.Size - 2f), grass);

            GameObject Solid(GameObject go)
            {
                go.AddComponent<BoxCollider>();
                return go;
            }

            // Iron fence all round; the gate is on the north side, on the street where the fare waits.
            Solid(Prim.Box(c, "Fence", new Vector3(0f, 0.9f, -half), new Vector3(half * 2f, 1.6f, 0.1f), iron));
            Solid(Prim.Box(c, "Fence", new Vector3(half, 0.9f, 0f), new Vector3(0.1f, 1.6f, half * 2f), iron));
            Solid(Prim.Box(c, "Fence", new Vector3(-half, 0.9f, 0f), new Vector3(0.1f, 1.6f, half * 2f), iron));
            var westLen = gateX - gateHalf + half;
            var eastLen = half - gateX - gateHalf;
            Solid(Prim.Box(c, "Fence", new Vector3(-half + westLen * 0.5f, 0.9f, half), new Vector3(westLen, 1.6f, 0.1f), iron));
            Solid(Prim.Box(c, "Fence", new Vector3(half - eastLen * 0.5f, 0.9f, half), new Vector3(eastLen, 1.6f, 0.1f), iron));

            Solid(Prim.Box(c, "GatePostW", new Vector3(gateX - gateHalf - 0.45f, 1.8f, half), new Vector3(0.9f, 3.4f, 0.9f), stone));
            Solid(Prim.Box(c, "GatePostE", new Vector3(gateX + gateHalf + 0.45f, 1.8f, half), new Vector3(0.9f, 3.4f, 0.9f), stone));
            Prim.Box(c, "GateArch", new Vector3(gateX, 3.5f, half), new Vector3(gateHalf * 2f + 1.8f, 0.5f, 0.3f), iron);
            var arch = Group(c, "GateName");
            arch.localPosition = new Vector3(gateX, 3.5f, half + 0.2f);
            arch.localRotation = Quaternion.LookRotation(Vector3.back); // read from the street to the north
            Text(arch, "MERCY HILL", Vector3.zero, 0.38f, new Color(0.75f, 0.75f, 0.7f));

            // Path from the gate, rows of stones either side, a mausoleum at the back.
            Prim.Box(c, "Path", new Vector3(gateX, 0.155f, half * 0.35f), new Vector3(2.4f, 0.02f, half * 1.3f), pavement);
            for (var i = 0; i < 48; i++)
            {
                var x = ((float)rng.NextDouble() - 0.5f) * (half * 2f - 6f);
                if (Mathf.Abs(x - gateX) < 3f) continue;
                var z = ((float)rng.NextDouble() - 0.5f) * (half * 2f - 14f) + 3f;
                var h = 0.7f + (float)rng.NextDouble() * 0.7f;
                var s = Prim.Box(c, "Tombstone", new Vector3(x, h * 0.5f + 0.14f, z), new Vector3(0.7f, h, 0.18f), stone);
                s.transform.localRotation = Quaternion.Euler(((float)rng.NextDouble() - 0.5f) * 8f, ((float)rng.NextDouble() - 0.5f) * 20f, 0f);
            }

            Solid(Prim.Box(c, "Mausoleum", new Vector3(0f, 2.1f, -half + 6f), new Vector3(7f, 4f, 5f), stone));
            Prim.Box(c, "MausoleumRoof", new Vector3(0f, 4.35f, -half + 6f), new Vector3(7.8f, 0.5f, 5.8f), iron);
            Prim.Box(c, "MausoleumDoor", new Vector3(0f, 1.3f, -half + 8.53f), new Vector3(1.6f, 2.4f, 0.06f), iron);
        }

        void BuildPark(Transform parent, BlockPlan block)
        {
            var p = Group(parent, "Park");
            p.position = block.Center;
            var inner = block.Size - 4f;
            Prim.Box(p, "Grass", new Vector3(0f, 0.13f, 0f), new Vector3(inner, 0.04f, inner), grass);
            Prim.Box(p, "PathNS", new Vector3(0f, 0.155f, 0f), new Vector3(2.4f, 0.02f, inner), pavement);
            Prim.Box(p, "PathEW", new Vector3(0f, 0.155f, 0f), new Vector3(inner, 0.02f, 2.4f), pavement);

            // A statue nobody remembers putting up.
            Prim.Box(p, "Plinth", new Vector3(0f, 0.8f, 0f), new Vector3(2.4f, 1.4f, 2.4f), stone).AddComponent<BoxCollider>();
            Prim.Make(PrimitiveType.Capsule, p, "Statue", new Vector3(0f, 2.6f, 0f), new Vector3(0.7f, 1.1f, 0.5f), stone);
            Prim.Sphere(p, "StatueHead", new Vector3(0f, 3.95f, 0.05f), Vector3.one * 0.38f, stone);

            foreach (var (pos, yaw) in new[] { (new Vector3(4f, 0f, 9f), 180f), (new Vector3(-4f, 0f, -9f), 0f), (new Vector3(9f, 0f, -4f), -90f), (new Vector3(-9f, 0f, 4f), 90f) })
            {
                var bench = Group(p, "Bench");
                bench.localPosition = pos;
                bench.localRotation = Quaternion.Euler(0f, yaw, 0f);
                Prim.Box(bench, "Seat", new Vector3(0f, 0.5f, 0f), new Vector3(1.8f, 0.08f, 0.5f), wood);
                Prim.Box(bench, "Back", new Vector3(0f, 0.85f, -0.22f), new Vector3(1.8f, 0.5f, 0.06f), wood);
            }

            Instance(prefabs.StreetLamp, p, block.Center + new Vector3(6f, 0f, 6f), Quaternion.LookRotation(new Vector3(-1f, 0f, -1f)));
        }

        void BuildParkingLot(Transform parent, BlockPlan block)
        {
            var lot = Group(parent, "RideshareLot");
            lot.position = block.Center;
            var inner = block.Size - 4f;
            Prim.Box(lot, "Asphalt", new Vector3(0f, 0.13f, 0f), new Vector3(inner, 0.04f, inner), lotAsphalt);
            for (var i = 0; i <= 4; i++)
            {
                var x = -24f + i * 12f;
                Prim.Box(lot, "Stall", new Vector3(x, 0.155f, -10f), new Vector3(0.15f, 0.02f, 5.5f), white);
                Prim.Box(lot, "Stall", new Vector3(x, 0.155f, 10f), new Vector3(0.15f, 0.02f, 5.5f), white);
            }

            // Attendant booth, long closed, with a sign for drivers.
            Prim.Box(lot, "Booth", new Vector3(-20f, 1.4f, 2f), new Vector3(2.4f, 2.6f, 2.4f), buildingShades[2]).AddComponent<BoxCollider>();
            Prim.Box(lot, "BoothWindow", new Vector3(-20f, 1.7f, 3.22f), new Vector3(1.6f, 0.9f, 0.04f), warmWindow);
            var sign = Group(lot, "RideshareSign");
            sign.localPosition = new Vector3(-20f, 3.2f, 3.3f);
            sign.localRotation = Quaternion.LookRotation(Vector3.back);
            Text(sign, "RIDESHARE PICKUP", Vector3.zero, 0.22f, new Color(0.4f, 0.85f, 1f));

            Instance(prefabs.StreetLamp, lot, block.Center + new Vector3(10f, 0f, 0f), Quaternion.LookRotation(Vector3.left));
            Instance(prefabs.StreetLamp, lot, block.Center + new Vector3(-10f, 0f, -2f), Quaternion.LookRotation(Vector3.right));
        }

        // -------------------------------------------------------------- Highways

        void BuildHighway(Transform parent, HighwayPlan highway)
        {
            var group = Group(parent, highway.Name);
            var surface = new MeshBatcher();
            var rails = new MeshBatcher();
            var w = WorldGenerator.HighwayWidth;
            var half = w * 0.5f;

            var length = 0f;
            for (var i = 1; i < highway.Nodes.Count; i++)
                length += Vector3.Distance(plan.Nodes[highway.Nodes[i - 1]], plan.Nodes[highway.Nodes[i]]);

            var s = 0f;
            for (var i = 1; i < highway.Nodes.Count; i++)
            {
                var a = plan.Nodes[highway.Nodes[i - 1]];
                var b = plan.Nodes[highway.Nodes[i]];
                var segment = Vector3.Distance(a, b);

                surface.Strip(a, b, 0f, w, 0.1f, 0.012f, highwayAsphalt);
                surface.Strip(a, b, -0.2f, 0.12f, 0.02f, 0.03f, yellow);
                surface.Strip(a, b, 0.2f, 0.12f, 0.02f, 0.03f, yellow);
                surface.Strip(a, b, -(half - 0.8f), 0.15f, 0.02f, 0.03f, white);
                surface.Strip(a, b, half - 0.8f, 0.15f, 0.02f, 0.03f, white);
                surface.Dashes(a, b, -3.6f, 0.15f, 4f, 12f, 0.03f, white, 2f);
                surface.Dashes(a, b, 3.6f, 0.15f, 4f, 12f, 0.03f, white, 2f);
                if (i < highway.Nodes.Count - 1) surface.Disc(b, w, 0.1f, 0.012f, highwayAsphalt);

                // Guardrails, left open near each end so the ramps blend into the streets.
                if (s > 25f && s + segment < length - 25f)
                {
                    rails.Strip(a, b, -(half + 0.6f), 0.15f, 0.7f, 0.85f, rail);
                    rails.Strip(a, b, half + 0.6f, 0.15f, 0.7f, 0.85f, rail);
                }

                s += segment;
            }

            surface.Build(group, "Surface");
            rails.Build(group, "Guardrails", withCollider: true);
        }

        // --------------------------------------------------------- Neighbourhoods

        void BuildNeighborhood(Transform parent, NeighborhoodPlan hood, int index)
        {
            var group = Group(parent, hood.Style.Name);
            var nodes = new HashSet<int>(hood.Nodes);
            var batch = new MeshBatcher();
            var w = WorldGenerator.StreetWidth;
            var walkOffset = w * 0.5f + 1.4f;

            foreach (var r in plan.Roads)
            {
                if (r.Kind != RoadKind.Street || !nodes.Contains(r.A) || !nodes.Contains(r.B)) continue;
                var a = plan.Nodes[r.A];
                var b = plan.Nodes[r.B];
                batch.Strip(a, b, 0f, w, 0.1f, 0.01f, asphalt);

                // Pavements on both sides, stopping short of junctions and turning circles.
                var dir = (b - a).normalized;
                var a2 = a + dir * Clearance(r.A);
                var b2 = b - dir * Clearance(r.B);
                if (Vector3.Dot(b2 - a2, dir) > 0.5f)
                {
                    batch.Strip(a2, b2, -walkOffset, 1.6f, 0.08f, 0.08f, pavement);
                    batch.Strip(a2, b2, walkOffset, 1.6f, 0.08f, 0.08f, pavement);
                }
            }

            foreach (var n in hood.Nodes)
            {
                if (touchesHighway[n]) continue; // the highway's own joint covers it
                var radius = plan.NodeRadius[n];
                batch.Disc(plan.Nodes[n], radius > 0f ? radius * 2f : w, 0.1f, 0.01f, asphalt);
            }

            batch.Build(group, "Streets");

            var houses = Group(group, "Houses");
            foreach (var lot in plan.Houses)
            {
                if (lot.Neighborhood != index) continue;
                var highlight = lot.Address == highlightAddress;
                var porch = highlight || rng.NextDouble() < hood.Style.PorchLightChance;
                var lit = highlight || rng.NextDouble() < hood.Style.LitWindowChance;
                var house = Instance(prefabs.PickHouse(porch, lit).Prefab, houses, lot.Position, lot.Rotation);
                house.name = lot.Address;
            }
        }

        float Clearance(int node)
        {
            if (plan.NodeRadius[node] > 0f) return plan.NodeRadius[node] + 1f;
            return degree[node] > 2 || touchesHighway[node] ? WorldGenerator.StreetWidth * 0.5f + 2.5f : 0f;
        }

        // -------------------------------------------------- Lamps, trees, signs

        void PlaceLamps()
        {
            var group = Group(root, "Lamps");
            foreach (var lamp in plan.Lamps)
                Instance(lamp.Kind == LampKind.Highway ? prefabs.HighwayLamp : prefabs.StreetLamp, group, lamp.Position, lamp.Rotation);
        }

        void PlaceTrees()
        {
            var group = Group(root, "Trees");
            foreach (var tree in plan.Trees)
            {
                var go = Instance(prefabs.Tree(tree.Kind), group, tree.Position, Quaternion.Euler(0f, tree.Yaw, 0f));
                go.transform.localScale = Vector3.one * tree.Scale;
            }
        }

        void PlaceSigns()
        {
            var group = Group(root, "Signs");
            foreach (var sign in plan.Signs)
            {
                var s = Group(group, $"Sign: {sign.Text.Replace('\n', ' ')}");
                s.SetPositionAndRotation(sign.Position, sign.Rotation);

                switch (sign.Kind)
                {
                    case SignKind.Highway:
                        foreach (var x in new[] { -3.8f, 3.8f })
                            Prim.Box(s, "Post", new Vector3(x, 2.9f, 0.1f), new Vector3(0.18f, 5.8f, 0.18f), iron).AddComponent<BoxCollider>();
                        Prim.Box(s, "Board", new Vector3(0f, 5.4f, 0f), new Vector3(9f, 2.4f, 0.15f), signGreen);
                        Text(s, sign.Text, new Vector3(0f, 5.4f, -0.1f), 0.75f, new Color(0.85f, 0.88f, 0.85f));
                        break;

                    case SignKind.Street:
                        Prim.Box(s, "Post", new Vector3(0f, 1.45f, 0f), new Vector3(0.08f, 2.9f, 0.08f), iron).AddComponent<BoxCollider>();
                        Prim.Box(s, "Plate", new Vector3(0f, 2.95f, 0f), new Vector3(2.6f, 0.38f, 0.05f), signGreen);
                        Text(s, sign.Text, new Vector3(0f, 2.95f, -0.04f), 0.24f, new Color(0.85f, 0.88f, 0.85f));
                        break;

                    case SignKind.Neighborhood:
                        Prim.Box(s, "Wall", new Vector3(0f, 0.7f, 0f), new Vector3(6.5f, 1.4f, 0.6f), stone).AddComponent<BoxCollider>();
                        Prim.Box(s, "Cap", new Vector3(0f, 1.47f, 0f), new Vector3(6.8f, 0.14f, 0.75f), iron);
                        Text(s, sign.Text, new Vector3(0f, 0.8f, -0.32f), 0.55f, new Color(0.8f, 0.78f, 0.7f));
                        Prim.PointLight(s, "Uplight", new Vector3(0f, 0.2f, -1.6f), new Color(1f, 0.85f, 0.6f), 5f, 1.5f);
                        break;
                }
            }
        }

        void PlaceProps()
        {
            var group = Group(root, "Props");
            foreach (var prop in plan.Props)
                Instance(prop.Kind == PropKind.AbandonedCar ? prefabs.AbandonedCar : prefabs.ParkedCar, group, prop.Position, prop.Rotation);
        }

        // --------------------------------------------------------------- Helpers

        /// <summary>World-space text, readable by someone looking along the parent's +Z.</summary>
        void Text(Transform parent, string text, Vector3 localPosition, float lineHeight, Color color)
        {
            var go = Child(parent, "Text");
            go.transform.localPosition = localPosition;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = font;
            tm.fontSize = 64;
            tm.characterSize = lineHeight * 10f / 64f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        static GameObject Instance(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.SetPositionAndRotation(position, rotation);
            return go;
        }

        static Transform Group(Transform parent, string name) => Child(parent, name).transform;

        static GameObject Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Static batching for the greybox; prefab instances already carry the flag from their asset.</summary>
        static void MarkStatic(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject)) continue;
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            }
        }
    }
}
