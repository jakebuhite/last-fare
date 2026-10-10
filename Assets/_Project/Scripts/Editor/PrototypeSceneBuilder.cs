using System.Collections.Generic;
using LastFare.Audio;
using LastFare.Data;
using LastFare.Driving;
using LastFare.Navigation;
using LastFare.Passengers;
using LastFare.Player;
using LastFare.Rides;
using LastFare.UI;
using LastFare.World;
using UnityEditor;
using UnityEngine;

namespace LastFare.Editor
{
    /// <summary>
    /// Builds the greybox first-fare prototype into the active scene as ordinary GameObjects: an open
    /// city grid at night, the drivable car with a working rear-view mirror, the passenger, hazards,
    /// GPS, audio, HUD and the FareDirector, all wired through serialized references. Generated
    /// materials, meshes and the mirror render texture are saved under <see cref="GeneratedDir"/>.
    /// Everything is placeholder art meant to be replaced piece by piece in the scene.
    /// </summary>
    public class PrototypeSceneBuilder
    {
        public const string GeneratedDir = "Assets/_Project/Art/Generated";

        // Map layout. Blocks and intersections are counted from the south-west corner.
        const int Columns = 7;            // intersections west to east (6 blocks)
        const int Rows = 7;               // intersections south to north (6 blocks)
        const float Block = 60f;          // intersection spacing
        const float RoadWidth = 10f;
        const float Sidewalk = 3f;
        const float LaneOffset = 2.5f;    // lane centre from the road centre
        static readonly Vector2Int CemeteryBlock = new(3, 4);
        static readonly Vector2Int HouseBlock = new(5, 0);

        readonly RideDefinition ride;
        CityMap map;
        System.Random rng;
        Transform worldRoot;

        public PrototypeSceneBuilder(RideDefinition ride, Material litMaterial, Material unlitMaterial)
        {
            this.ride = ride;
            Prim.Init(litMaterial, unlitMaterial);
        }

        /// <summary>Builds everything into the active scene. <see cref="GeneratedDir"/> must exist.</summary>
        public void Build()
        {
            rng = new System.Random(1234);

            map = new GameObject("CityMap").AddComponent<CityMap>();
            map.transform.position = new Vector3(-(Columns - 1) * Block * 0.5f, 0f, -(Rows - 1) * Block * 0.5f);
            Wire(map, ("columns", Columns), ("rows", Rows), ("blockSize", Block), ("roadWidth", RoadWidth));

            SetupAtmosphere();

            worldRoot = new GameObject("Environment").transform;
            BuildGround();
            BuildRoads();
            BuildStreetLamps();
            BuildCityBlocks();
            BuildBoundary();
            BuildCemetery();
            BuildHouse();
            MarkStatic(worldRoot);

            // Stops: pickup on the road in front of the cemetery gate, drop-off in front of the house.
            var north = Vector3.forward;
            var pickupRoad = map.Intersection(CemeteryBlock.x + 0.5f, CemeteryBlock.y);
            var dropoffRoad = map.Intersection(HouseBlock.x + 0.5f, HouseBlock.y + 1);

            var stops = new GameObject("Stops").transform;
            var pickupPoint = Point(stops, "PickupPoint", pickupRoad + north * LaneOffset);
            var dropoffPoint = Point(stops, "DropoffPoint", dropoffRoad - north * LaneOffset);
            var standPoint = Point(stops, "PickupStandPoint", pickupRoad + north * (RoadWidth * 0.5f + 1.5f) + Vector3.left * 1.2f);
            var lookPoint = Point(stops, "PickupLookPoint", pickupRoad + Vector3.left * 1.2f);
            var pickupMarker = BuildMarker(stops, "PickupMarker", pickupRoad + north * (RoadWidth * 0.5f + 0.5f), new Color(0.45f, 0.85f, 1f));
            var dropoffMarker = BuildMarker(stops, "DropoffMarker", dropoffRoad - north * (RoadWidth * 0.5f + 0.5f), new Color(0.55f, 1f, 0.6f));

            // Start on the south edge of the map, eastbound in the right-hand lane.
            var carParts = BuildCar(map.Intersection(0.5f, 0f) + Vector3.back * LaneOffset, Quaternion.LookRotation(Vector3.right));
            var passenger = BuildPassenger(carParts.Car.transform, carParts.Eye);
            var hazards = BuildHazards();

            var systems = new GameObject("Systems").transform;
            var sound = Child(systems, "Audio").AddComponent<LastFareAudio>();
            var hud = Child(systems, "HUD").AddComponent<HudView>();
            var gpsGo = Child(systems, "GPS");
            var gps = gpsGo.AddComponent<GpsNavigator>();
            var gpsView = gpsGo.AddComponent<GpsView>();
            var directorGo = Child(systems, "FareDirector");
            var controller = directorGo.AddComponent<RideController>();
            var director = directorGo.AddComponent<FareDirector>();

            Wire(gps, ("map", map), ("vehicle", carParts.Car.transform));
            Wire(gpsView, ("gps", gps));
            Wire(hud, ("director", director), ("car", carParts.Car));
            Wire(director,
                ("ride", ride),
                ("controller", controller),
                ("car", carParts.Car),
                ("look", carParts.Look),
                ("passenger", passenger),
                ("sound", sound),
                ("hud", hud),
                ("gps", gps),
                ("hazards", hazards),
                ("pickupMarker", pickupMarker),
                ("dropoffMarker", dropoffMarker),
                ("pickupPoint", pickupPoint),
                ("dropoffPoint", dropoffPoint),
                ("pickupStandPoint", standPoint),
                ("pickupLookPoint", lookPoint));

            // Hidden until the fare reaches them.
            pickupMarker.SetActive(false);
            dropoffMarker.SetActive(false);
        }

        // ------------------------------------------------------------ Atmosphere

        static void SetupAtmosphere()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.045f, 0.075f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.022f;
            RenderSettings.fogColor = new Color(0.03f, 0.035f, 0.05f);

            var moon = new GameObject("Moonlight").AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.45f, 0.55f, 0.85f);
            moon.intensity = 0.12f;
            moon.shadows = LightShadows.None;
            moon.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // ----------------------------------------------------------------- World

        void BuildGround()
        {
            Prim.Box(worldRoot, "Ground", new Vector3(0f, -0.12f, 0f), new Vector3(1400f, 0.2f, 1400f),
                Prim.Lit(new Color(0.045f, 0.05f, 0.045f), 0.02f));
        }

        void BuildRoads()
        {
            var roads = Child(worldRoot, "Roads").transform;
            var lines = Child(roads, "CentreLines").transform;
            var asphalt = Prim.Lit(new Color(0.07f, 0.07f, 0.08f), 0.45f);
            var paint = Prim.Lit(new Color(0.75f, 0.62f, 0.2f), 0.1f);
            var net = map.Network;

            // One piece per street and per intersection, so each gets lit by the lamps near it.
            for (var e = 0; e < net.EdgeCount; e++)
            {
                var (ia, ib) = net.Edge(e);
                Vector3 a = net.Node(ia), b = net.Node(ib);
                var dir = (b - a).normalized;
                var rot = Quaternion.LookRotation(dir);
                var length = Block - RoadWidth;

                var street = Prim.Box(roads, "Street", (a + b) * 0.5f, new Vector3(RoadWidth, 0.02f, length), asphalt);
                street.transform.rotation = rot;

                for (var d = 2f; d + 3f <= length; d += 8f)
                {
                    var p = a + dir * (RoadWidth * 0.5f + d + 1.5f) + Vector3.up * 0.015f;
                    Prim.Box(lines, "Dash", p, new Vector3(0.16f, 0.02f, 3f), paint).transform.rotation = rot;
                }
            }

            for (var n = 0; n < net.NodeCount; n++)
                Prim.Box(roads, "Intersection", net.Node(n), new Vector3(RoadWidth, 0.02f, RoadWidth), asphalt);
        }

        void BuildStreetLamps()
        {
            var lamps = Child(worldRoot, "StreetLamps").transform;
            var pole = Prim.Lit(new Color(0.08f, 0.08f, 0.09f), 0.2f);
            var bulb = Prim.Unlit(new Color(1f, 0.8f, 0.45f));
            var net = map.Network;
            var corner = RoadWidth * 0.5f + 1f;

            for (var n = 0; n < net.NodeCount; n++)
            {
                // One lamp per intersection on the north-east corner, arm reaching over the crossing.
                var node = net.Node(n);
                var basePoint = node + new Vector3(corner, 0f, corner);
                var toCentre = (node - basePoint).normalized;

                var root = Child(lamps, "Lamp").transform;
                root.SetPositionAndRotation(basePoint, Quaternion.LookRotation(toCentre) * Quaternion.Euler(0f, 90f, 0f));

                var p = Prim.Box(root, "Pole", new Vector3(0f, 3.2f, 0f), new Vector3(0.14f, 6.4f, 0.14f), pole);
                p.AddComponent<BoxCollider>();
                Prim.Box(root, "Arm", new Vector3(-0.9f, 6.35f, 0f), new Vector3(1.8f, 0.1f, 0.1f), pole);
                Prim.Box(root, "Bulb", new Vector3(-1.7f, 6.25f, 0f), new Vector3(0.6f, 0.12f, 0.3f), bulb);
                Prim.PointLight(root, "Light", new Vector3(-1.7f, 5.9f, 0f), new Color(1f, 0.72f, 0.38f), 26f, 8f);
            }
        }

        void BuildCityBlocks()
        {
            var blocks = Child(worldRoot, "Blocks").transform;
            var concrete = Prim.Lit(new Color(0.09f, 0.09f, 0.1f), 0.1f);
            var window = Prim.Unlit(new Color(1f, 0.72f, 0.35f) * 0.85f);

            // The playable blocks plus a ring of blocks around them that walls the map in.
            for (var bz = -1; bz < Rows; bz++)
            {
                for (var bx = -1; bx < Columns; bx++)
                {
                    var current = new Vector2Int(bx, bz);
                    if (current == CemeteryBlock || current == HouseBlock) continue;

                    var outer = bx < 0 || bz < 0 || bx >= Columns - 1 || bz >= Rows - 1;
                    var block = Child(blocks, $"Block_{bx}_{bz}").transform;
                    block.position = map.BlockCenter(bx, bz);
                    Prim.Box(block, "Sidewalk", new Vector3(0f, 0.04f, 0f), new Vector3(Block - RoadWidth, 0.08f, Block - RoadWidth), concrete);
                    BuildLots(block, outer, window);
                }
            }
        }

        void BuildLots(Transform block, bool outer, Material window)
        {
            var lot = (Block - RoadWidth - Sidewalk * 2f) * 0.5f;

            for (var lz = 0; lz < 2; lz++)
            {
                for (var lx = 0; lx < 2; lx++)
                {
                    if (!outer && rng.NextDouble() < 0.08) continue; // the odd empty lot

                    var side = new Vector3(lx * 2 - 1, 0f, lz * 2 - 1); // which corner of the block
                    var width = lot - 1f - (float)rng.NextDouble() * 4f;
                    var depth = lot - 1f - (float)rng.NextDouble() * 4f;
                    var height = outer ? 14f + (float)rng.NextDouble() * 20f : 6f + (float)rng.NextDouble() * 22f;
                    var shade = 0.05f + (int)(rng.NextDouble() * 4) * 0.02f; // a few shades, so a few materials

                    // Push each building out to the street so the block reads as a wall of facades.
                    var centre = block.position + new Vector3(side.x * (lot - width * 0.5f), height * 0.5f, side.z * (lot - depth * 0.5f));
                    var b = Prim.Box(block, "Building", Vector3.zero, Vector3.one,
                        Prim.Lit(new Color(shade, shade * 1.05f, shade * 1.2f), 0.05f));
                    b.transform.position = centre;
                    b.transform.localScale = new Vector3(width, height, depth);
                    b.AddComponent<BoxCollider>();

                    if (rng.NextDouble() < 0.5)
                    {
                        // A lit window on one of the street-facing sides.
                        var onX = rng.NextDouble() < 0.5;
                        var wy = 2f + (float)rng.NextDouble() * Mathf.Max(1f, height - 4f);
                        var offset = onX
                            ? new Vector3(side.x * (width * 0.5f + 0.02f), 0f, 0f)
                            : new Vector3(0f, 0f, side.z * (depth * 0.5f + 0.02f));
                        var size = onX ? new Vector3(0.1f, 1.3f, 1.4f) : new Vector3(1.4f, 1.3f, 0.1f);
                        var w = Prim.Box(block, "Window", Vector3.zero, size, window);
                        w.transform.position = centre + offset + Vector3.up * (wy - height * 0.5f);
                        w.transform.SetParent(b.transform, true);
                    }
                }
            }
        }

        void BuildBoundary()
        {
            // Invisible walls just outside the ring of outer blocks, in case a gap lets the car through.
            var walls = Child(worldRoot, "BoundaryWalls").transform;
            var min = map.Intersection(-1f, -1f);
            var max = map.Intersection(Columns, Rows);
            var size = max - min;
            var centre = (min + max) * 0.5f;

            void Wall(string name, Vector3 pos, Vector3 scale)
            {
                var go = Child(walls, name);
                go.transform.position = pos + Vector3.up * 5f;
                go.AddComponent<BoxCollider>().size = scale;
            }

            Wall("North", new Vector3(centre.x, 0f, max.z), new Vector3(size.x, 10f, 1f));
            Wall("South", new Vector3(centre.x, 0f, min.z), new Vector3(size.x, 10f, 1f));
            Wall("East", new Vector3(max.x, 0f, centre.z), new Vector3(1f, 10f, size.z));
            Wall("West", new Vector3(min.x, 0f, centre.z), new Vector3(1f, 10f, size.z));
        }

        void BuildCemetery()
        {
            var cemetery = Child(worldRoot, "Cemetery").transform;
            cemetery.position = map.BlockCenter(CemeteryBlock.x, CemeteryBlock.y);
            var grass = Prim.Lit(new Color(0.04f, 0.06f, 0.04f), 0.02f);
            var stone = Prim.Lit(new Color(0.16f, 0.17f, 0.18f), 0.1f);
            var iron = Prim.Lit(new Color(0.05f, 0.05f, 0.06f), 0.3f);

            var inner = Block - RoadWidth;
            var half = inner * 0.5f - 1f;
            const float gateHalf = 2.5f;
            Prim.Box(cemetery, "Grass", new Vector3(0f, 0.03f, 0f), new Vector3(inner, 0.06f, inner), grass);

            GameObject Solid(GameObject go)
            {
                go.AddComponent<BoxCollider>();
                return go;
            }

            // Fence on all four sides, with the gate gap facing the south road where the pickup is.
            Solid(Prim.Box(cemetery, "Fence", new Vector3(0f, 0.8f, half), new Vector3(half * 2f, 1.6f, 0.1f), iron));
            Solid(Prim.Box(cemetery, "Fence", new Vector3(half, 0.8f, 0f), new Vector3(0.1f, 1.6f, half * 2f), iron));
            Solid(Prim.Box(cemetery, "Fence", new Vector3(-half, 0.8f, 0f), new Vector3(0.1f, 1.6f, half * 2f), iron));
            var southLength = half - gateHalf;
            Solid(Prim.Box(cemetery, "Fence", new Vector3(-(gateHalf + southLength * 0.5f), 0.8f, -half), new Vector3(southLength, 1.6f, 0.1f), iron));
            Solid(Prim.Box(cemetery, "Fence", new Vector3(gateHalf + southLength * 0.5f, 0.8f, -half), new Vector3(southLength, 1.6f, 0.1f), iron));

            Solid(Prim.Box(cemetery, "GatePostA", new Vector3(-gateHalf - 0.45f, 1.7f, -half), new Vector3(0.9f, 3.4f, 0.9f), stone));
            Solid(Prim.Box(cemetery, "GatePostB", new Vector3(gateHalf + 0.45f, 1.7f, -half), new Vector3(0.9f, 3.4f, 0.9f), stone));
            Prim.Box(cemetery, "GateArch", new Vector3(0f, 3.4f, -half), new Vector3(gateHalf * 2f + 1.8f, 0.3f, 0.4f), iron);

            for (var i = 0; i < 40; i++)
            {
                var x = ((float)rng.NextDouble() - 0.5f) * (half * 2f - 6f);
                var z = ((float)rng.NextDouble() - 0.5f) * (half * 2f - 10f) + 2f;
                var h = 0.7f + (float)rng.NextDouble() * 0.7f;
                var s = Prim.Box(cemetery, "Tombstone", new Vector3(x, h * 0.5f, z), new Vector3(0.7f, h, 0.18f), stone);
                s.transform.localRotation = Quaternion.Euler(0f, ((float)rng.NextDouble() - 0.5f) * 20f, 0f);
            }
        }

        void BuildHouse()
        {
            var house = Child(worldRoot, "DropoffHouse").transform;
            house.position = map.BlockCenter(HouseBlock.x, HouseBlock.y);
            var yard = Prim.Lit(new Color(0.05f, 0.06f, 0.045f), 0.02f);
            var wall = Prim.Lit(new Color(0.12f, 0.11f, 0.1f), 0.05f);
            var roof = Prim.Lit(new Color(0.06f, 0.05f, 0.05f), 0.05f);
            var warm = Prim.Unlit(new Color(1f, 0.74f, 0.4f) * 0.9f);

            var inner = Block - RoadWidth;
            Prim.Box(house, "Yard", new Vector3(0f, 0.03f, 0f), new Vector3(inner, 0.06f, inner), yard);

            // The house faces north, onto the road where the drop-off is.
            var front = inner * 0.5f - Sidewalk - 8f;
            var centre = new Vector3(0f, 0f, front - 4f);
            Prim.Box(house, "Walls", centre + Vector3.up * 2.5f, new Vector3(9f, 5f, 8f), wall).AddComponent<BoxCollider>();
            Prim.Box(house, "Roof", centre + Vector3.up * 5.4f, new Vector3(10f, 0.8f, 9f), roof);
            Prim.Box(house, "Window", centre + new Vector3(2f, 2.6f, 4.02f), new Vector3(1.6f, 1.4f, 0.1f), warm);
            Prim.Box(house, "Door", centre + new Vector3(-1.5f, 1.1f, 4.02f), new Vector3(1.1f, 2.2f, 0.1f), roof);
            Prim.PointLight(house, "PorchLight", centre + new Vector3(-1.5f, 3f, 5.5f), new Color(1f, 0.75f, 0.4f), 12f, 5f);
        }

        static GameObject BuildMarker(Transform parent, string name, Vector3 position, Color color)
        {
            var root = Child(parent, name);
            root.transform.position = position;
            Prim.Make(PrimitiveType.Cylinder, root.transform, "Beam", new Vector3(0f, 4f, 0f), new Vector3(0.25f, 4f, 0.25f), Prim.Unlit(color));
            Prim.PointLight(root.transform, "Glow", new Vector3(0f, 2f, 0f), color, 9f, 4f);
            return root;
        }

        // ------------------------------------------------------------------- Car

        struct CarParts
        {
            public CarController Car;
            public DriverLook Look;
            public Transform Eye;
        }

        static CarParts BuildCar(Vector3 position, Quaternion rotation)
        {
            var carGo = new GameObject("Car");
            var t = carGo.transform;
            t.SetPositionAndRotation(position, rotation);

            var body = carGo.AddComponent<Rigidbody>();
            body.mass = 1300f;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            var hull = carGo.AddComponent<BoxCollider>();
            hull.center = new Vector3(0f, 0.9f, 0.4f);
            hull.size = new Vector3(1.9f, 1.5f, 4.6f);
            var car = carGo.AddComponent<CarController>();

            var shell = Prim.Lit(new Color(0.06f, 0.06f, 0.07f), 0.1f);
            var trim = Prim.Lit(new Color(0.1f, 0.1f, 0.12f), 0.2f);
            var seat = Prim.Lit(new Color(0.14f, 0.12f, 0.12f), 0.05f);
            var glow = Prim.Unlit(new Color(0.2f, 0.95f, 0.55f));

            // Body
            var shellRoot = Child(t, "Body").transform;
            Prim.Box(shellRoot, "Floor", new Vector3(0f, 0.25f, -0.1f), new Vector3(1.8f, 0.1f, 3.4f), shell);
            Prim.Box(shellRoot, "Roof", new Vector3(0f, 1.62f, -0.5f), new Vector3(1.8f, 0.06f, 2.1f), shell);
            Prim.Box(shellRoot, "Hood", new Vector3(0f, 0.88f, 2.0f), new Vector3(1.7f, 0.1f, 1.4f), shell);
            Prim.Box(shellRoot, "Dash", new Vector3(0f, 0.85f, 0.9f), new Vector3(1.8f, 0.38f, 0.55f), trim);
            Prim.Box(shellRoot, "Cluster", new Vector3(-0.4f, 1.0f, 0.64f), new Vector3(0.32f, 0.07f, 0.02f), glow);
            Prim.Box(shellRoot, "RearPanel", new Vector3(0f, 0.8f, -1.95f), new Vector3(1.8f, 0.9f, 0.1f), shell);
            foreach (var side in new[] { -1f, 1f })
            {
                Prim.Box(shellRoot, "Door", new Vector3(side * 0.92f, 0.7f, -0.45f), new Vector3(0.08f, 0.6f, 2.8f), trim);
                Prim.Box(shellRoot, "PillarA", new Vector3(side * 0.88f, 1.2f, 0.5f), new Vector3(0.07f, 0.85f, 0.07f), shell);
                Prim.Box(shellRoot, "PillarB", new Vector3(side * 0.9f, 1.25f, -0.6f), new Vector3(0.08f, 0.8f, 0.12f), shell);
                Prim.Box(shellRoot, "PillarC", new Vector3(side * 0.85f, 1.25f, -1.85f), new Vector3(0.08f, 0.8f, 0.12f), shell);
            }

            // Seats (tops stay below eye height so looking back is not blocked)
            var seats = Child(t, "Seats").transform;
            foreach (var x in new[] { -0.4f, 0.4f })
            {
                Prim.Box(seats, "SeatBase", new Vector3(x, 0.36f, -0.15f), new Vector3(0.5f, 0.18f, 0.5f), seat);
                Prim.Box(seats, "SeatBack", new Vector3(x, 0.78f, -0.45f), new Vector3(0.5f, 0.62f, 0.12f), seat);
            }

            Prim.Box(seats, "RearBase", new Vector3(0f, 0.35f, -1.2f), new Vector3(1.7f, 0.2f, 0.55f), seat);
            Prim.Box(seats, "RearBack", new Vector3(0f, 0.85f, -1.58f), new Vector3(1.7f, 0.72f, 0.12f), seat);

            BuildSteeringWheel(t, trim);

            // Interior light: a dim dashboard glow and a cold fill for the back seat.
            var lights = Child(t, "Lights").transform;
            Prim.PointLight(lights, "DashGlow", new Vector3(0f, 1.05f, 0.7f), new Color(0.35f, 0.85f, 0.65f), 3.4f, 1.1f);
            Prim.PointLight(lights, "RearFill", new Vector3(0f, 1.45f, -1.0f), new Color(0.55f, 0.6f, 0.9f), 3.4f, 0.45f);

            // Headlights
            var headGo = Child(lights, "Headlights");
            headGo.transform.localPosition = new Vector3(0f, 0.8f, 2.7f);
            headGo.transform.localRotation = Quaternion.Euler(3f, 0f, 0f);
            var head = headGo.AddComponent<Light>();
            head.type = LightType.Spot;
            head.range = 50f;
            head.spotAngle = 75f;
            head.innerSpotAngle = 35f;
            head.intensity = 70f;
            head.color = new Color(1f, 0.93f, 0.8f);
            head.shadows = LightShadows.None;

            // Driver's head and camera
            var headPivot = Child(t, "DriverHead");
            headPivot.transform.localPosition = new Vector3(-0.4f, 1.25f, 0.05f);
            var look = headPivot.AddComponent<DriverLook>();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(headPivot.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 72f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 220f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            camGo.AddComponent<AudioListener>();

            var mirror = BuildMirror(t);
            Wire(look, ("cam", cam), ("mirrorTarget", mirror));

            return new CarParts { Car = car, Look = look, Eye = camGo.transform };
        }

        static void BuildSteeringWheel(Transform car, Material mat)
        {
            var wheel = Child(car, "SteeringWheel").transform;
            wheel.localPosition = new Vector3(-0.4f, 0.98f, 0.5f);
            wheel.localRotation = Quaternion.Euler(25f, 0f, 0f);

            const int segments = 14;
            const float radius = 0.17f;
            for (var i = 0; i < segments; i++)
            {
                var a = i / (float)segments * Mathf.PI * 2f;
                Prim.Box(wheel, "Rim", new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f),
                    new Vector3(0.09f, 0.035f, 0.035f), mat, new Vector3(0f, 0f, a * Mathf.Rad2Deg + 90f));
            }

            Prim.Box(wheel, "Hub", Vector3.zero, new Vector3(0.09f, 0.09f, 0.04f), mat);
            Prim.Box(wheel, "Spoke", Vector3.zero, new Vector3(0.32f, 0.03f, 0.03f), mat);
        }

        static Transform BuildMirror(Transform car)
        {
            const float aspect = 3.2f;
            var root = Child(car, "RearViewMirror").transform;
            var housing = Prim.Lit(new Color(0.04f, 0.04f, 0.05f), 0.1f);
            var mirrorPos = new Vector3(0f, 1.42f, 0.8f);

            Prim.Box(root, "MirrorStalk", mirrorPos + new Vector3(0f, 0.1f, 0.03f), new Vector3(0.03f, 0.2f, 0.03f), housing);
            Prim.Box(root, "MirrorHousing", mirrorPos + new Vector3(0f, 0f, 0.012f), new Vector3(0.34f, 0.125f, 0.02f), housing);

            var rt = new RenderTexture(512, Mathf.RoundToInt(512 / aspect), 16) { name = "MirrorRT" };
            AssetDatabase.CreateAsset(rt, GeneratedDir + "/MirrorRT.renderTexture");

            var mat = Prim.Unlit(Color.white);
            mat.name = "Mirror";
            mat.SetTexture("_BaseMap", rt);
            mat.SetTextureScale("_BaseMap", new Vector2(-1f, 1f)); // a mirror flips the image horizontally
            mat.SetTextureOffset("_BaseMap", new Vector2(1f, 0f));
            AssetDatabase.CreateAsset(mat, GeneratedDir + "/Mirror.mat");

            var quad = Prim.Make(PrimitiveType.Quad, root, "MirrorGlass", mirrorPos, new Vector3(0.3f, 0.3f / aspect, 1f), mat);

            var camGo = Child(root, "MirrorCamera");
            camGo.transform.localPosition = mirrorPos + new Vector3(0f, 0f, -0.02f);
            camGo.transform.localRotation = Quaternion.Euler(6f, 180f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.targetTexture = rt;
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 90f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;

            return quad.transform;
        }

        // ------------------------------------------------------------- Passenger

        static PassengerActor BuildPassenger(Transform car, Transform eye)
        {
            var go = new GameObject("Passenger_QuietOne");
            var actor = go.AddComponent<PassengerActor>();
            actor.Build(car, eye);
            return actor;
        }

        /// <summary>Hazard figures start hidden; the FareDirector stands them along the drop-off route.</summary>
        static RoadHazard[] BuildHazards()
        {
            var root = new GameObject("Hazards").transform;
            var hazards = new RoadHazard[3];

            var skin = Prim.Lit(new Color(0.75f, 0.74f, 0.7f), 0.05f);
            var coat = Prim.Lit(new Color(0.2f, 0.19f, 0.2f), 0.05f);

            for (var i = 0; i < hazards.Length; i++)
            {
                var go = Child(root, $"Hazard_{i}");
                Prim.Box(go.transform, "Body", new Vector3(0f, 1.05f, 0f), new Vector3(0.3f, 1.9f, 0.18f), coat);
                Prim.Box(go.transform, "ArmL", new Vector3(-0.22f, 1.0f, 0f), new Vector3(0.07f, 1.3f, 0.07f), coat);
                Prim.Box(go.transform, "ArmR", new Vector3(0.22f, 1.0f, 0f), new Vector3(0.07f, 1.3f, 0.07f), coat);
                Prim.Sphere(go.transform, "Head", new Vector3(0f, 2.15f, 0f), Vector3.one * 0.24f, skin);

                hazards[i] = go.AddComponent<RoadHazard>();
                go.SetActive(false);
            }

            return hazards;
        }

        // --------------------------------------------------------------- Helpers

        static GameObject Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        static Transform Point(Transform parent, string name, Vector3 position)
        {
            var t = Child(parent, name).transform;
            t.position = position;
            return t;
        }

        static void MarkStatic(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }

        /// <summary>Assigns private [SerializeField] fields by name, failing loudly if one was renamed.</summary>
        static void Wire(Object target, params (string field, object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in values)
            {
                var prop = so.FindProperty(field)
                           ?? throw new System.ArgumentException($"{target.GetType().Name} has no serialized field '{field}'.");
                switch (value)
                {
                    case Object obj:
                        prop.objectReferenceValue = obj;
                        break;
                    case float f:
                        prop.floatValue = f;
                        break;
                    case int n:
                        prop.intValue = n;
                        break;
                    case Object[] array:
                        prop.arraySize = array.Length;
                        for (var i = 0; i < array.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
                        break;
                    default:
                        throw new System.ArgumentException($"Unsupported value for {target.GetType().Name}.{field}.");
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Saves every material and mesh under <paramref name="roots"/> that isn't an asset yet into
        /// <see cref="GeneratedDir"/>, merging identical materials so each colour exists once.
        /// </summary>
        public static void SaveGeneratedAssets(IEnumerable<GameObject> roots)
        {
            var materials = new Dictionary<string, Material>();

            foreach (var root in roots)
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var shared = renderer.sharedMaterials;
                    for (var i = 0; i < shared.Length; i++)
                    {
                        var m = shared[i];
                        if (m == null || AssetDatabase.Contains(m)) continue;

                        var key = MaterialKey(m);
                        if (!materials.TryGetValue(key, out var saved))
                        {
                            m.name = key;
                            AssetDatabase.CreateAsset(m, $"{GeneratedDir}/{key}.mat");
                            materials[key] = saved = m;
                        }

                        shared[i] = saved;
                    }

                    renderer.sharedMaterials = shared;
                }

                foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter.sharedMesh;
                    if (mesh != null && !AssetDatabase.Contains(mesh))
                        AssetDatabase.CreateAsset(mesh, $"{GeneratedDir}/{mesh.name}.asset");
                }
            }
        }

        static string MaterialKey(Material m)
        {
            var lit = m.shader.name.EndsWith("/Lit");
            var key = $"{(lit ? "Lit" : "Unlit")}_{ColorUtility.ToHtmlStringRGB(m.GetColor("_BaseColor"))}";
            if (lit) key += $"_s{Mathf.RoundToInt(m.GetFloat("_Smoothness") * 100f):00}";
            return key;
        }
    }
}
