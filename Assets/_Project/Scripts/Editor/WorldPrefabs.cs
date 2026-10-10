using System.Collections.Generic;
using LastFare.World;
using UnityEditor;
using UnityEngine;

namespace LastFare.Editor
{
    /// <summary>
    /// Greybox prefabs the world is dressed with: house variants, trees, lamp posts and cars. Each is a
    /// prefab asset so the scene stays small and any of them can be swapped for real art in one place.
    /// </summary>
    public class WorldPrefabs
    {
        public class House
        {
            public GameObject Prefab;
            public bool PorchLight;
            public bool LitWindows;
        }

        public readonly List<House> Houses = new();
        public GameObject Pine, RoundTree, DeadTree, StreetLamp, HighwayLamp, ParkedCar, AbandonedCar;

        static readonly Color[] WallColors =
        {
            new(0.32f, 0.30f, 0.27f), new(0.24f, 0.27f, 0.30f), new(0.26f, 0.28f, 0.24f),
            new(0.30f, 0.20f, 0.17f), new(0.34f, 0.34f, 0.33f), new(0.22f, 0.20f, 0.18f),
        };

        static readonly Color[] RoofColors = { new(0.08f, 0.08f, 0.09f), new(0.12f, 0.09f, 0.08f), new(0.10f, 0.11f, 0.12f) };

        readonly GeneratedAssets assets;
        readonly System.Random rng;

        WorldPrefabs(GeneratedAssets assets, System.Random rng)
        {
            this.assets = assets;
            this.rng = rng;
        }

        public static WorldPrefabs Create(GeneratedAssets assets, System.Random rng)
        {
            var p = new WorldPrefabs(assets, rng);

            // 16 houses: every combination of porch light and lit windows, four shapes each.
            for (var i = 0; i < 16; i++)
            {
                var porch = (i & 1) != 0;
                var lit = (i & 2) != 0;
                p.Houses.Add(new House { Prefab = p.Save(p.BuildHouse(porch, lit), $"House_{i:00}"), PorchLight = porch, LitWindows = lit });
            }

            p.Pine = p.Save(p.BuildPine(), "Tree_Pine");
            p.RoundTree = p.Save(p.BuildRoundTree(), "Tree_Round");
            p.DeadTree = p.Save(p.BuildDeadTree(), "Tree_Dead");
            p.StreetLamp = p.Save(BuildLamp("StreetLamp", 6.4f, 1.8f, 24f, 7f), "StreetLamp");
            p.HighwayLamp = p.Save(BuildLamp("HighwayLamp", 9.5f, 3f, 34f, 12f), "HighwayLamp");
            p.ParkedCar = p.Save(BuildCar("ParkedCar", new Color(0.16f, 0.18f, 0.22f), false), "ParkedCar");
            p.AbandonedCar = p.Save(BuildCar("AbandonedCar", new Color(0.2f, 0.12f, 0.08f), true), "AbandonedCar");
            return p;
        }

        public GameObject Tree(TreeKind kind) => kind switch
        {
            TreeKind.Pine => Pine,
            TreeKind.Dead => DeadTree,
            _ => RoundTree,
        };

        /// <summary>A house variant matching the rolled look; falls back to any house.</summary>
        public House PickHouse(bool porch, bool lit)
        {
            var matches = Houses.FindAll(h => h.PorchLight == porch && h.LitWindows == lit);
            if (matches.Count == 0) matches = Houses;
            return matches[rng.Next(matches.Count)];
        }

        GameObject Save(GameObject template, string name)
        {
            foreach (var t in template.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
            return assets.SavePrefab(template, name);
        }

        float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);

        // ----------------------------------------------------------------- House

        /// <summary>Origin at the front wall's centre on the ground; the front faces +Z, toward the street.</summary>
        GameObject BuildHouse(bool porch, bool lit)
        {
            var root = new GameObject("House").transform;
            var w = Range(9f, 13f);
            var d = Range(8f, 11f);
            var twoStory = rng.NextDouble() < 0.35;
            var h = twoStory ? 5.6f : 3.1f;

            var wall = Prim.Lit(WallColors[rng.Next(WallColors.Length)], 0.05f);
            var roof = Prim.Lit(RoofColors[rng.Next(RoofColors.Length)], 0.1f);
            var trim = Prim.Lit(new Color(0.18f, 0.17f, 0.16f), 0.1f);
            var door = Prim.Lit(new Color(0.12f, 0.07f, 0.05f), 0.2f);
            var glass = Prim.Lit(new Color(0.04f, 0.05f, 0.07f), 0.8f);
            var warm = Prim.Unlit(new Color(1f, 0.74f, 0.4f) * 0.85f);
            var concrete = Prim.Lit(new Color(0.14f, 0.14f, 0.14f), 0.05f);

            Prim.Box(root, "Walls", new Vector3(0f, h * 0.5f, -d * 0.5f), new Vector3(w, h, d), wall).AddComponent<BoxCollider>();

            // Gable roof: two slabs meeting over the middle, the triangle ends filled by a squashed diamond.
            const float pitch = 30f;
            var tan = Mathf.Tan(pitch * Mathf.Deg2Rad);
            var roofRoot = new GameObject("Roof").transform;
            roofRoot.SetParent(root, false);
            roofRoot.localPosition = new Vector3(0f, h, -d * 0.5f);
            var slab = d * 0.5f / Mathf.Cos(pitch * Mathf.Deg2Rad) + 0.5f;
            Prim.Box(roofRoot, "Front", new Vector3(0f, d * 0.25f * tan + 0.1f, d * 0.25f), new Vector3(w + 0.6f, 0.18f, slab), roof, new Vector3(pitch, 0f, 0f));
            Prim.Box(roofRoot, "Back", new Vector3(0f, d * 0.25f * tan + 0.1f, -d * 0.25f), new Vector3(w + 0.6f, 0.18f, slab), roof, new Vector3(-pitch, 0f, 0f));
            var gable = new GameObject("Gable").transform;
            gable.SetParent(roofRoot, false);
            gable.localScale = new Vector3(1f, tan, 1f);
            Prim.Box(gable, "Fill", Vector3.zero, new Vector3(w, d / Mathf.Sqrt(2f), d / Mathf.Sqrt(2f)), wall, new Vector3(45f, 0f, 0f));

            if (rng.NextDouble() < 0.4)
                Prim.Box(root, "Chimney", new Vector3(w * 0.3f, h + d * 0.2f * tan + 1f, -d * 0.7f), new Vector3(0.8f, 2.4f, 0.8f), roof);

            // Front: door, windows (some lit), maybe an upstairs row.
            var doorX = -w * 0.15f;
            Prim.Box(root, "Door", new Vector3(doorX, 1.05f, 0.04f), new Vector3(1f, 2.1f, 0.08f), door);
            var anyLit = false;
            void Window(float x, float y)
            {
                var on = lit && (!anyLit || rng.NextDouble() < 0.5);
                anyLit |= on;
                Prim.Box(root, on ? "WindowLit" : "Window", new Vector3(x, y, 0.03f), new Vector3(1.3f, 1.1f, 0.06f), on ? warm : glass);
            }

            Window(w * 0.25f, 1.5f);
            Window(-w * 0.38f, 1.5f);
            if (twoStory)
            {
                Window(w * 0.25f, 4.2f);
                Window(-w * 0.25f, 4.2f);
            }

            // Garage on one side, driveway down to the street.
            var side = rng.NextDouble() < 0.5 ? -1f : 1f;
            var driveX = side * (w * 0.5f - 1.8f);
            if (rng.NextDouble() < 0.6)
            {
                var gx = side * (w * 0.5f + 2.4f);
                Prim.Box(root, "Garage", new Vector3(gx, 1.4f, -3.1f), new Vector3(4.6f, 2.8f, 6.2f), wall).AddComponent<BoxCollider>();
                Prim.Box(root, "GarageRoof", new Vector3(gx, 2.88f, -3.1f), new Vector3(4.9f, 0.15f, 6.5f), roof);
                Prim.Box(root, "GarageDoor", new Vector3(gx, 1.15f, 0.03f), new Vector3(3.8f, 2.3f, 0.06f), trim);
                driveX = gx;
            }

            var yard = WorldGenerator.FrontYard;
            Prim.Box(root, "Driveway", new Vector3(driveX, 0.02f, yard * 0.5f), new Vector3(3.4f, 0.04f, yard + 0.5f), concrete);
            Prim.Box(root, "Walk", new Vector3(doorX, 0.015f, (yard - 2.2f) * 0.5f), new Vector3(1.1f, 0.03f, yard - 2.2f), concrete);

            var mailX = driveX + side * 2.3f;
            Prim.Box(root, "MailboxPost", new Vector3(mailX, 0.5f, yard - 0.9f), new Vector3(0.08f, 1f, 0.08f), trim);
            Prim.Box(root, "Mailbox", new Vector3(mailX, 1.05f, yard - 0.9f), new Vector3(0.25f, 0.25f, 0.45f), roof);

            if (porch)
            {
                Prim.Box(root, "PorchLamp", new Vector3(doorX + 0.9f, 2.3f, 0.08f), new Vector3(0.18f, 0.25f, 0.12f), warm);
                Prim.PointLight(root, "PorchLight", new Vector3(doorX + 0.9f, 2.2f, 0.8f), new Color(1f, 0.72f, 0.4f), 7f, 2.5f);
            }

            return root.gameObject;
        }

        // ----------------------------------------------------------------- Trees

        GameObject BuildPine()
        {
            var root = new GameObject("Pine").transform;
            var bark = Prim.Lit(new Color(0.09f, 0.07f, 0.05f), 0.05f);
            var needles = Prim.Lit(new Color(0.05f, 0.09f, 0.06f), 0.05f);
            Prim.Make(PrimitiveType.Cylinder, root, "Trunk", new Vector3(0f, 1.2f, 0f), new Vector3(0.4f, 1.2f, 0.4f), bark);
            Prim.Sphere(root, "Canopy1", new Vector3(0f, 3.4f, 0f), new Vector3(3.4f, 2.4f, 3.4f), needles);
            Prim.Sphere(root, "Canopy2", new Vector3(0f, 5f, 0f), new Vector3(2.6f, 2.2f, 2.6f), needles);
            Prim.Sphere(root, "Canopy3", new Vector3(0f, 6.5f, 0f), new Vector3(1.6f, 2f, 1.6f), needles);
            AddTrunkCollider(root, 0.35f, 3f);
            return root.gameObject;
        }

        GameObject BuildRoundTree()
        {
            var root = new GameObject("RoundTree").transform;
            var bark = Prim.Lit(new Color(0.1f, 0.08f, 0.06f), 0.05f);
            var leaves = Prim.Lit(new Color(0.07f, 0.1f, 0.06f), 0.05f);
            Prim.Make(PrimitiveType.Cylinder, root, "Trunk", new Vector3(0f, 1.6f, 0f), new Vector3(0.45f, 1.6f, 0.45f), bark);
            Prim.Sphere(root, "Canopy", new Vector3(0f, 4.6f, 0f), new Vector3(4.6f, 3.8f, 4.6f), leaves);
            Prim.Sphere(root, "Canopy2", new Vector3(0.9f, 5.6f, 0.6f), new Vector3(3.2f, 2.8f, 3.2f), leaves);
            AddTrunkCollider(root, 0.4f, 3.2f);
            return root.gameObject;
        }

        GameObject BuildDeadTree()
        {
            var root = new GameObject("DeadTree").transform;
            var bark = Prim.Lit(new Color(0.12f, 0.1f, 0.09f), 0.05f);
            Prim.Make(PrimitiveType.Cylinder, root, "Trunk", new Vector3(0f, 2.4f, 0f), new Vector3(0.35f, 2.4f, 0.35f), bark);
            Prim.Box(root, "Branch1", new Vector3(0.6f, 4.2f, 0f), new Vector3(0.14f, 2.2f, 0.14f), bark, new Vector3(0f, 0f, -35f));
            Prim.Box(root, "Branch2", new Vector3(-0.6f, 3.6f, 0.2f), new Vector3(0.12f, 1.8f, 0.12f), bark, new Vector3(10f, 0f, 40f));
            Prim.Box(root, "Branch3", new Vector3(0.1f, 4.8f, -0.5f), new Vector3(0.1f, 1.6f, 0.1f), bark, new Vector3(-35f, 0f, 0f));
            AddTrunkCollider(root, 0.3f, 4.8f);
            return root.gameObject;
        }

        static void AddTrunkCollider(Transform root, float radius, float height)
        {
            var c = root.gameObject.AddComponent<CapsuleCollider>();
            c.radius = radius;
            c.height = height;
            c.center = new Vector3(0f, height * 0.5f, 0f);
        }

        // ------------------------------------------------------------ Lamps, cars

        /// <summary>A lamp post at the origin with its arm reaching along +Z.</summary>
        static GameObject BuildLamp(string name, float height, float reach, float range, float intensity)
        {
            var root = new GameObject(name).transform;
            var pole = Prim.Lit(new Color(0.08f, 0.08f, 0.09f), 0.2f);
            var bulb = Prim.Unlit(new Color(1f, 0.8f, 0.45f));
            Prim.Box(root, "Pole", new Vector3(0f, height * 0.5f, 0f), new Vector3(0.16f, height, 0.16f), pole).AddComponent<BoxCollider>();
            Prim.Box(root, "Arm", new Vector3(0f, height - 0.05f, reach * 0.5f), new Vector3(0.1f, 0.1f, reach), pole);
            Prim.Box(root, "Bulb", new Vector3(0f, height - 0.15f, reach - 0.1f), new Vector3(0.3f, 0.12f, 0.6f), bulb);
            Prim.PointLight(root, "Light", new Vector3(0f, height - 0.5f, reach - 0.1f), new Color(1f, 0.68f, 0.35f), range, intensity);
            return root.gameObject;
        }

        static GameObject BuildCar(string name, Color paint, bool abandoned)
        {
            var root = new GameObject(name).transform;
            var body = Prim.Lit(paint, 0.4f);
            var glass = Prim.Lit(new Color(0.03f, 0.04f, 0.05f), 0.8f);
            var tyre = Prim.Lit(new Color(0.03f, 0.03f, 0.03f), 0.1f);

            Prim.Box(root, "Body", new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 0.7f, 4.4f), body);
            Prim.Box(root, "Cabin", new Vector3(0f, 1.18f, -0.2f), new Vector3(1.6f, 0.5f, 2.2f), body);
            Prim.Box(root, "Windows", new Vector3(0f, 1.2f, -0.2f), new Vector3(1.64f, 0.34f, 2.0f), glass);
            foreach (var x in new[] { -0.85f, 0.85f })
            foreach (var z in new[] { -1.4f, 1.4f })
                Prim.Make(PrimitiveType.Cylinder, root, "Tyre", new Vector3(x, 0.32f, z), new Vector3(0.62f, 0.12f, 0.62f), tyre, new Vector3(0f, 0f, 90f));

            var hull = root.gameObject.AddComponent<BoxCollider>();
            hull.center = new Vector3(0f, 0.75f, 0f);
            hull.size = new Vector3(1.8f, 1.5f, 4.4f);

            if (abandoned)
            {
                // Hazard lights left on, the driver's door hanging open.
                var amber = Prim.Unlit(new Color(1f, 0.55f, 0.1f));
                foreach (var x in new[] { -0.75f, 0.75f })
                {
                    Prim.Box(root, "Hazard", new Vector3(x, 0.75f, 2.21f), new Vector3(0.25f, 0.12f, 0.04f), amber);
                    Prim.Box(root, "Hazard", new Vector3(x, 0.75f, -2.21f), new Vector3(0.25f, 0.12f, 0.04f), amber);
                }

                Prim.PointLight(root, "HazardGlow", new Vector3(0f, 0.8f, -2.8f), new Color(1f, 0.55f, 0.1f), 5f, 1.5f);
                Prim.Box(root, "OpenDoor", new Vector3(-1.45f, 0.9f, 0.35f), new Vector3(0.08f, 0.9f, 1.1f), body, new Vector3(0f, -55f, 0f));
            }

            return root.gameObject;
        }
    }
}
