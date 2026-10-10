using System.Collections.Generic;
using LastFare.Audio;
using LastFare.Data;
using LastFare.Driving;
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
    /// Builds the greybox first-fare prototype into the active scene as ordinary GameObjects: night road
    /// loop, the car interior with a working rear-view mirror, the passenger, hazards, audio, HUD and the
    /// FareDirector, all wired through serialized references. Generated materials, meshes and the mirror
    /// render texture are saved under <see cref="GeneratedDir"/>. Everything is placeholder art meant to
    /// be replaced piece by piece in the scene.
    /// </summary>
    public class PrototypeSceneBuilder
    {
        public const string GeneratedDir = "Assets/_Project/Art/Generated";

        const float StartDistance = 40f;
        const float PickupDistance = 250f;
        const float RoadHalfWidth = 4f;

        readonly RideDefinition ride;
        RoutePath route;
        float dropoffDistance;
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

            var routeLoop = new GameObject("Route").AddComponent<RouteLoop>();
            route = routeLoop.Path;
            dropoffDistance = Mathf.Min(1050f, route.Length - 80f);

            SetupAtmosphere();

            worldRoot = new GameObject("Environment").transform;
            BuildGround();
            BuildRoad();
            BuildStreetLamps();
            BuildBuildings();
            BuildCemetery();
            BuildHouse();
            MarkStatic(worldRoot);

            var carParts = BuildCar(routeLoop);
            var passenger = BuildPassenger(carParts.Car.transform, carParts.Eye);
            var hazards = BuildHazards();

            var stops = new GameObject("Stops").transform;
            var pickupMarker = BuildMarker(stops, "PickupMarker", route.OffsetPoint(PickupDistance, 5.2f), new Color(0.45f, 0.85f, 1f));
            var dropoffMarker = BuildMarker(stops, "DropoffMarker", route.OffsetPoint(dropoffDistance, 5.2f), new Color(0.55f, 1f, 0.6f));
            var standPoint = Point(stops, "PickupStandPoint", route.OffsetPoint(PickupDistance - 1.2f, 5.2f));
            var lookPoint = Point(stops, "PickupLookPoint", route.OffsetPoint(PickupDistance - 1.2f, 0f));

            var systems = new GameObject("Systems").transform;
            var sound = Child(systems, "Audio").AddComponent<LastFareAudio>();
            var hud = Child(systems, "HUD").AddComponent<HudView>();
            var directorGo = Child(systems, "FareDirector");
            var controller = directorGo.AddComponent<RideController>();
            var director = directorGo.AddComponent<FareDirector>();

            Wire(hud, ("director", director), ("car", carParts.Car));
            Wire(director,
                ("ride", ride),
                ("controller", controller),
                ("car", carParts.Car),
                ("look", carParts.Look),
                ("passenger", passenger),
                ("sound", sound),
                ("hud", hud),
                ("hazards", hazards),
                ("pickupMarker", pickupMarker),
                ("dropoffMarker", dropoffMarker),
                ("pickupStandPoint", standPoint),
                ("pickupLookPoint", lookPoint),
                ("pickupDistance", PickupDistance),
                ("dropoffDistance", dropoffDistance));

            // Hidden until the fare reaches them; shown here would only clutter the scene view.
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

        void BuildRoad()
        {
            var go = Child(worldRoot, "Road");

            var step = 4f;
            var n = Mathf.CeilToInt(route.Length / step);
            var verts = new Vector3[(n + 1) * 2];
            var tris = new int[n * 6];

            for (var i = 0; i <= n; i++)
            {
                route.Evaluate(i * route.Length / n, out var p, out var f);
                var right = Vector3.Cross(Vector3.up, f);
                verts[i * 2] = p - right * RoadHalfWidth + Vector3.up * 0.01f;
                verts[i * 2 + 1] = p + right * RoadHalfWidth + Vector3.up * 0.01f;
            }

            for (var i = 0; i < n; i++)
            {
                int a = i * 2, b = i * 2 + 1, c = i * 2 + 2, d = i * 2 + 3;
                var t = i * 6;
                tris[t] = a; tris[t + 1] = c; tris[t + 2] = b;
                tris[t + 3] = b; tris[t + 4] = c; tris[t + 5] = d;
            }

            var mesh = new Mesh { name = "RoadMesh", vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Prim.Lit(new Color(0.07f, 0.07f, 0.08f), 0.45f);

            var lines = Child(go.transform, "CentreLines").transform;
            var line = Prim.Lit(new Color(0.75f, 0.62f, 0.2f), 0.1f);
            for (var d = 0f; d < route.Length; d += 9f)
            {
                route.Evaluate(d, out var p, out var f);
                Prim.Box(lines, "Line", p + Vector3.up * 0.03f, new Vector3(0.16f, 0.02f, 3.4f), line)
                    .transform.rotation = Quaternion.LookRotation(f);
            }
        }

        void BuildStreetLamps()
        {
            var lamps = Child(worldRoot, "StreetLamps").transform;
            var pole = Prim.Lit(new Color(0.08f, 0.08f, 0.09f), 0.2f);
            var bulb = Prim.Unlit(new Color(1f, 0.8f, 0.45f));

            for (var d = 20f; d < route.Length; d += 42f)
            {
                var basePoint = route.OffsetPoint(d, RoadHalfWidth + 1.2f);
                route.Evaluate(d, out _, out var f);
                var root = Child(lamps, "Lamp").transform;
                root.SetPositionAndRotation(basePoint, Quaternion.LookRotation(f));

                Prim.Box(root, "Pole", new Vector3(0f, 3.2f, 0f), new Vector3(0.14f, 6.4f, 0.14f), pole);
                Prim.Box(root, "Arm", new Vector3(-0.9f, 6.35f, 0f), new Vector3(1.8f, 0.1f, 0.1f), pole);
                Prim.Box(root, "Bulb", new Vector3(-1.7f, 6.25f, 0f), new Vector3(0.6f, 0.12f, 0.3f), bulb);
                Prim.PointLight(root, "Light", new Vector3(-1.7f, 5.9f, 0f), new Color(1f, 0.72f, 0.38f), 24f, 7f);
            }
        }

        void BuildBuildings()
        {
            var buildings = Child(worldRoot, "Buildings").transform;
            var window = Prim.Unlit(new Color(1f, 0.72f, 0.35f) * 0.85f);

            for (var d = 0f; d < route.Length; d += 26f)
            {
                for (var side = -1; side <= 1; side += 2)
                {
                    // Keep the cemetery frontage and the dropoff house clear.
                    if (side > 0 && (Mathf.Abs(d - PickupDistance) < 40f || Mathf.Abs(d - dropoffDistance) < 25f)) continue;

                    var offset = side * (RoadHalfWidth + 12f + (float)rng.NextDouble() * 18f);
                    var width = 8f + (float)rng.NextDouble() * 10f;
                    var height = 6f + (float)rng.NextDouble() * 22f;
                    var depth = 8f + (float)rng.NextDouble() * 8f;
                    var shade = 0.05f + (int)(rng.NextDouble() * 4) * 0.02f; // a few shades, so a few materials

                    route.Evaluate(d, out var p, out var f);
                    var right = Vector3.Cross(Vector3.up, f);
                    var pos = p + right * offset + Vector3.up * (height * 0.5f);
                    var rot = Quaternion.LookRotation(f);

                    var b = Prim.Box(buildings, "Building", pos, new Vector3(width, height, depth),
                        Prim.Lit(new Color(shade, shade * 1.05f, shade * 1.2f), 0.05f));
                    b.transform.rotation = rot;

                    if (rng.NextDouble() < 0.4)
                    {
                        var wy = 2f + (float)rng.NextDouble() * Mathf.Max(1f, height - 4f);
                        var wp = pos + right * (-side * (width * 0.5f + 0.02f)) + Vector3.up * (wy - height * 0.5f);
                        var w = Prim.Box(b.transform, "Window", Vector3.zero, Vector3.one, window);
                        w.transform.SetPositionAndRotation(wp, rot);
                        w.transform.localScale = new Vector3(0.1f / width, 1.3f / height, 1.4f / depth);
                    }
                }
            }
        }

        void BuildCemetery()
        {
            var cemetery = Child(worldRoot, "Cemetery").transform;
            var stone = Prim.Lit(new Color(0.16f, 0.17f, 0.18f), 0.1f);
            var iron = Prim.Lit(new Color(0.05f, 0.05f, 0.06f), 0.3f);

            route.Evaluate(PickupDistance, out _, out var f);
            var rot = Quaternion.LookRotation(f);
            var gate = route.OffsetPoint(PickupDistance, 9.5f);

            Prim.Box(cemetery, "GatePostA", gate + f * 2.4f + Vector3.up * 1.7f, new Vector3(0.9f, 3.4f, 0.9f), stone);
            Prim.Box(cemetery, "GatePostB", gate - f * 2.4f + Vector3.up * 1.7f, new Vector3(0.9f, 3.4f, 0.9f), stone);
            Prim.Box(cemetery, "GateArch", gate + Vector3.up * 3.4f, new Vector3(0.4f, 0.3f, 5.7f), iron).transform.rotation = rot;
            Prim.Box(cemetery, "Fence", gate + f * 24f + Vector3.up * 0.8f, new Vector3(0.1f, 1.6f, 38f), iron).transform.rotation = rot;
            Prim.Box(cemetery, "Fence", gate - f * 24f + Vector3.up * 0.8f, new Vector3(0.1f, 1.6f, 38f), iron).transform.rotation = rot;

            for (var i = 0; i < 16; i++)
            {
                var along = ((float)rng.NextDouble() - 0.5f) * 40f;
                var back = 13f + (float)rng.NextDouble() * 12f;
                var pos = route.OffsetPoint(PickupDistance + along, back) + Vector3.up * 0.5f;
                var h = 0.7f + (float)rng.NextDouble() * 0.7f;
                var s = Prim.Box(cemetery, "Tombstone", pos + Vector3.up * (h * 0.5f - 0.5f), new Vector3(0.7f, h, 0.18f), stone);
                s.transform.rotation = rot * Quaternion.Euler(0f, ((float)rng.NextDouble() - 0.5f) * 20f, 0f);
            }
        }

        void BuildHouse()
        {
            var house = Child(worldRoot, "DropoffHouse").transform;
            route.Evaluate(dropoffDistance, out _, out var f);
            var rot = Quaternion.LookRotation(f);
            var center = route.OffsetPoint(dropoffDistance, 15f);
            var wall = Prim.Lit(new Color(0.12f, 0.11f, 0.1f), 0.05f);
            var roof = Prim.Lit(new Color(0.06f, 0.05f, 0.05f), 0.05f);
            var warm = Prim.Unlit(new Color(1f, 0.74f, 0.4f) * 0.9f);

            Prim.Box(house, "Walls", center + Vector3.up * 2.5f, new Vector3(9f, 5f, 8f), wall).transform.rotation = rot;
            Prim.Box(house, "Roof", center + Vector3.up * 5.4f, new Vector3(10f, 0.8f, 9f), roof).transform.rotation = rot;

            var roadFacing = Vector3.Cross(Vector3.up, f); // right of the road; the house door faces back toward the road (-right)
            Prim.Box(house, "Window", center - roadFacing * 4.52f + Vector3.up * 2.6f, new Vector3(0.1f, 1.4f, 1.6f), warm).transform.rotation = rot;
            Prim.PointLight(house, "PorchLight", center - roadFacing * 5.5f + Vector3.up * 3f, new Color(1f, 0.75f, 0.4f), 12f, 5f);
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

        CarParts BuildCar(RouteLoop routeLoop)
        {
            var carGo = new GameObject("Car");
            var car = carGo.AddComponent<CarController>();
            var t = carGo.transform;

            var shell = Prim.Lit(new Color(0.06f, 0.06f, 0.07f), 0.1f);
            var trim = Prim.Lit(new Color(0.1f, 0.1f, 0.12f), 0.2f);
            var seat = Prim.Lit(new Color(0.14f, 0.12f, 0.12f), 0.05f);
            var glow = Prim.Unlit(new Color(0.2f, 0.95f, 0.55f));

            // Body
            var body = Child(t, "Body").transform;
            Prim.Box(body, "Floor", new Vector3(0f, 0.25f, -0.1f), new Vector3(1.8f, 0.1f, 3.4f), shell);
            Prim.Box(body, "Roof", new Vector3(0f, 1.62f, -0.5f), new Vector3(1.8f, 0.06f, 2.1f), shell);
            Prim.Box(body, "Hood", new Vector3(0f, 0.88f, 2.0f), new Vector3(1.7f, 0.1f, 1.4f), shell);
            Prim.Box(body, "Dash", new Vector3(0f, 0.85f, 0.9f), new Vector3(1.8f, 0.38f, 0.55f), trim);
            Prim.Box(body, "Cluster", new Vector3(-0.4f, 1.0f, 0.64f), new Vector3(0.32f, 0.07f, 0.02f), glow);
            Prim.Box(body, "RearPanel", new Vector3(0f, 0.8f, -1.95f), new Vector3(1.8f, 0.9f, 0.1f), shell);
            foreach (var side in new[] { -1f, 1f })
            {
                Prim.Box(body, "Door", new Vector3(side * 0.92f, 0.7f, -0.45f), new Vector3(0.08f, 0.6f, 2.8f), trim);
                Prim.Box(body, "PillarA", new Vector3(side * 0.88f, 1.2f, 0.5f), new Vector3(0.07f, 0.85f, 0.07f), shell);
                Prim.Box(body, "PillarB", new Vector3(side * 0.9f, 1.25f, -0.6f), new Vector3(0.08f, 0.8f, 0.12f), shell);
                Prim.Box(body, "PillarC", new Vector3(side * 0.85f, 1.25f, -1.85f), new Vector3(0.08f, 0.8f, 0.12f), shell);
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

            Wire(car, ("route", routeLoop), ("startDistance", StartDistance));
            car.Init(route, StartDistance); // also places the car at the start in the editor
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

        RoadHazard[] BuildHazards()
        {
            var root = new GameObject("Hazards").transform;
            var distances = new[] { 470f, 670f, 880f };
            var hazards = new RoadHazard[distances.Length];

            var skin = Prim.Lit(new Color(0.75f, 0.74f, 0.7f), 0.05f);
            var coat = Prim.Lit(new Color(0.2f, 0.19f, 0.2f), 0.05f);

            for (var i = 0; i < distances.Length; i++)
            {
                var go = Child(root, $"Hazard_{i}");
                Prim.Box(go.transform, "Body", new Vector3(0f, 1.05f, 0f), new Vector3(0.3f, 1.9f, 0.18f), coat);
                Prim.Box(go.transform, "ArmL", new Vector3(-0.22f, 1.0f, 0f), new Vector3(0.07f, 1.3f, 0.07f), coat);
                Prim.Box(go.transform, "ArmR", new Vector3(0.22f, 1.0f, 0f), new Vector3(0.07f, 1.3f, 0.07f), coat);
                Prim.Sphere(go.transform, "Head", new Vector3(0f, 2.15f, 0f), Vector3.one * 0.24f, skin);

                var hazard = go.AddComponent<RoadHazard>();
                hazard.Place(route, distances[i]);
                hazards[i] = hazard;
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
