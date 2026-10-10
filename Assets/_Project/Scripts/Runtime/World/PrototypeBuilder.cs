using LastFare.Audio;
using LastFare.Data;
using LastFare.Driving;
using LastFare.Passengers;
using LastFare.Player;
using LastFare.Rides;
using LastFare.UI;
using UnityEngine;

namespace LastFare.World
{
    /// <summary>
    /// Builds the whole greybox prototype at runtime: night road loop, the car interior with a working
    /// rear-view mirror, the passenger, hazards, audio and HUD. Drop this on an empty GameObject in an
    /// empty scene and press Play. Everything here is placeholder art meant to be replaced piece by piece.
    /// </summary>
    public class PrototypeBuilder : MonoBehaviour
    {
        const float StartDistance = 40f;
        const float PickupDistance = 250f;
        const float RoadHalfWidth = 4f;

        [SerializeField] RideDefinition ride;
        [SerializeField] Material litMaterial;
        [SerializeField] Material unlitMaterial;

        RoutePath route;
        float dropoffDistance;
        System.Random rng;
        Transform worldRoot;

        public RoutePath Route => route;

        void Awake() => Build();

        public void Build()
        {
            Prim.Init(litMaterial, unlitMaterial);
            rng = new System.Random(1234);

            if (ride == null) ride = CreateFallbackRide();

            route = RoutePath.CreateLoop(200f);
            dropoffDistance = Mathf.Min(1050f, route.Length - 80f);

            SetupAtmosphere();

            worldRoot = new GameObject("World").transform;
            BuildGround();
            BuildRoad();
            BuildStreetLamps();
            BuildBuildings();
            BuildCemetery();
            BuildHouse();

            var carParts = BuildCar();
            var passenger = BuildPassenger(carParts.Car.transform, carParts.Eye);
            var hazards = BuildHazards();
            var pickupMarker = BuildMarker("PickupMarker", route.OffsetPoint(PickupDistance, 5.2f), new Color(0.45f, 0.85f, 1f));
            var dropoffMarker = BuildMarker("DropoffMarker", route.OffsetPoint(dropoffDistance, 5.2f), new Color(0.55f, 1f, 0.6f));

            var sound = new GameObject("Audio").AddComponent<LastFareAudio>();
            var hud = new GameObject("HUD").AddComponent<HudView>();
            var director = new GameObject("FareDirector").AddComponent<FareDirector>();
            hud.Init(director, carParts.Car);

            director.Init(new FareDirector.Wiring
            {
                Ride = ride,
                Car = carParts.Car,
                Look = carParts.Look,
                Passenger = passenger,
                Sound = sound,
                Hud = hud,
                Hazards = hazards,
                Mirror = carParts.MirrorTarget,
                PickupMarker = pickupMarker,
                DropoffMarker = dropoffMarker,
                PickupStandPoint = route.OffsetPoint(PickupDistance - 1.2f, 5.2f),
                RoadSideLookPoint = route.OffsetPoint(PickupDistance - 1.2f, 0f),
                PickupDistance = PickupDistance,
                DropoffDistance = dropoffDistance
            });
        }

        // ------------------------------------------------------------ Atmosphere

        void SetupAtmosphere()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.045f, 0.075f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.022f;
            RenderSettings.fogColor = new Color(0.03f, 0.035f, 0.05f);

            var moon = new GameObject("Moon").AddComponent<Light>();
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
            var go = new GameObject("Road");
            go.transform.SetParent(worldRoot, false);

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

            var line = Prim.Lit(new Color(0.75f, 0.62f, 0.2f), 0.1f);
            for (var d = 0f; d < route.Length; d += 9f)
            {
                route.Evaluate(d, out var p, out var f);
                Prim.Box(worldRoot, "Line", p + Vector3.up * 0.03f, new Vector3(0.16f, 0.02f, 3.4f), line)
                    .transform.rotation = Quaternion.LookRotation(f);
            }
        }

        void BuildStreetLamps()
        {
            var pole = Prim.Lit(new Color(0.08f, 0.08f, 0.09f), 0.2f);
            var bulb = Prim.Unlit(new Color(1f, 0.8f, 0.45f));

            for (var d = 20f; d < route.Length; d += 42f)
            {
                var basePoint = route.OffsetPoint(d, RoadHalfWidth + 1.2f);
                route.Evaluate(d, out _, out var f);
                var root = new GameObject("Lamp").transform;
                root.SetParent(worldRoot, false);
                root.SetPositionAndRotation(basePoint, Quaternion.LookRotation(f));

                Prim.Box(root, "Pole", new Vector3(0f, 3.2f, 0f), new Vector3(0.14f, 6.4f, 0.14f), pole);
                Prim.Box(root, "Arm", new Vector3(-0.9f, 6.35f, 0f), new Vector3(1.8f, 0.1f, 0.1f), pole);
                Prim.Box(root, "Bulb", new Vector3(-1.7f, 6.25f, 0f), new Vector3(0.6f, 0.12f, 0.3f), bulb);
                Prim.PointLight(root, "Light", new Vector3(-1.7f, 5.9f, 0f), new Color(1f, 0.72f, 0.38f), 24f, 7f);
            }
        }

        void BuildBuildings()
        {
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
                    var shade = 0.05f + (float)rng.NextDouble() * 0.06f;

                    route.Evaluate(d, out var p, out var f);
                    var right = Vector3.Cross(Vector3.up, f);
                    var pos = p + right * offset + Vector3.up * (height * 0.5f);
                    var rot = Quaternion.LookRotation(f);

                    var b = Prim.Box(worldRoot, "Building", pos, new Vector3(width, height, depth),
                        Prim.Lit(new Color(shade, shade * 1.05f, shade * 1.2f), 0.05f));
                    b.transform.rotation = rot;

                    if (rng.NextDouble() < 0.4)
                    {
                        var wy = 2f + (float)rng.NextDouble() * Mathf.Max(1f, height - 4f);
                        var wp = pos + right * (-side * (width * 0.5f + 0.02f)) + Vector3.up * (wy - height * 0.5f);
                        var w = Prim.Box(worldRoot, "Window", wp, new Vector3(0.1f, 1.3f, 1.4f), window);
                        w.transform.rotation = rot;
                    }
                }
            }
        }

        void BuildCemetery()
        {
            var stone = Prim.Lit(new Color(0.16f, 0.17f, 0.18f), 0.1f);
            var iron = Prim.Lit(new Color(0.05f, 0.05f, 0.06f), 0.3f);

            route.Evaluate(PickupDistance, out _, out var f);
            var rot = Quaternion.LookRotation(f);
            var gate = route.OffsetPoint(PickupDistance, 9.5f);

            Prim.Box(worldRoot, "GatePostA", gate + f * 2.4f + Vector3.up * 1.7f, new Vector3(0.9f, 3.4f, 0.9f), stone);
            Prim.Box(worldRoot, "GatePostB", gate - f * 2.4f + Vector3.up * 1.7f, new Vector3(0.9f, 3.4f, 0.9f), stone);
            Prim.Box(worldRoot, "GateArch", gate + Vector3.up * 3.4f, new Vector3(0.4f, 0.3f, 5.7f), iron).transform.rotation = rot;
            Prim.Box(worldRoot, "Fence", gate + f * 24f + Vector3.up * 0.8f, new Vector3(0.1f, 1.6f, 38f), iron).transform.rotation = rot;
            Prim.Box(worldRoot, "Fence", gate - f * 24f + Vector3.up * 0.8f, new Vector3(0.1f, 1.6f, 38f), iron).transform.rotation = rot;

            for (var i = 0; i < 16; i++)
            {
                var along = ((float)rng.NextDouble() - 0.5f) * 40f;
                var back = 13f + (float)rng.NextDouble() * 12f;
                var pos = route.OffsetPoint(PickupDistance + along, back) + Vector3.up * 0.5f;
                var h = 0.7f + (float)rng.NextDouble() * 0.7f;
                var s = Prim.Box(worldRoot, "Tombstone", pos + Vector3.up * (h * 0.5f - 0.5f), new Vector3(0.7f, h, 0.18f), stone);
                s.transform.rotation = rot * Quaternion.Euler(0f, ((float)rng.NextDouble() - 0.5f) * 20f, 0f);
            }
        }

        void BuildHouse()
        {
            route.Evaluate(dropoffDistance, out _, out var f);
            var rot = Quaternion.LookRotation(f);
            var center = route.OffsetPoint(dropoffDistance, 15f);
            var wall = Prim.Lit(new Color(0.12f, 0.11f, 0.1f), 0.05f);
            var roof = Prim.Lit(new Color(0.06f, 0.05f, 0.05f), 0.05f);
            var warm = Prim.Unlit(new Color(1f, 0.74f, 0.4f) * 0.9f);

            Prim.Box(worldRoot, "House", center + Vector3.up * 2.5f, new Vector3(9f, 5f, 8f), wall).transform.rotation = rot;
            Prim.Box(worldRoot, "Roof", center + Vector3.up * 5.4f, new Vector3(10f, 0.8f, 9f), roof).transform.rotation = rot;

            var roadFacing = Vector3.Cross(Vector3.up, f); // right of the road; the house door faces back toward the road (-right)
            Prim.Box(worldRoot, "Window", center - roadFacing * 4.52f + Vector3.up * 2.6f, new Vector3(0.1f, 1.4f, 1.6f), warm).transform.rotation = rot;
            Prim.PointLight(worldRoot, "PorchLight", center - roadFacing * 5.5f + Vector3.up * 3f, new Color(1f, 0.75f, 0.4f), 12f, 5f);
        }

        GameObject BuildMarker(string name, Vector3 position, Color color)
        {
            var root = new GameObject(name);
            root.transform.SetParent(worldRoot, false);
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
            public Transform MirrorTarget;
            public Transform Eye;
        }

        CarParts BuildCar()
        {
            var carGo = new GameObject("Car");
            var car = carGo.AddComponent<CarController>();
            var t = carGo.transform;

            var shell = Prim.Lit(new Color(0.06f, 0.06f, 0.07f), 0.1f);
            var trim = Prim.Lit(new Color(0.1f, 0.1f, 0.12f), 0.2f);
            var seat = Prim.Lit(new Color(0.14f, 0.12f, 0.12f), 0.05f);
            var glow = Prim.Unlit(new Color(0.2f, 0.95f, 0.55f));

            // Body
            Prim.Box(t, "Floor", new Vector3(0f, 0.25f, -0.1f), new Vector3(1.8f, 0.1f, 3.4f), shell);
            Prim.Box(t, "Roof", new Vector3(0f, 1.62f, -0.5f), new Vector3(1.8f, 0.06f, 2.1f), shell);
            Prim.Box(t, "Hood", new Vector3(0f, 0.88f, 2.0f), new Vector3(1.7f, 0.1f, 1.4f), shell);
            Prim.Box(t, "Dash", new Vector3(0f, 0.85f, 0.9f), new Vector3(1.8f, 0.38f, 0.55f), trim);
            Prim.Box(t, "Cluster", new Vector3(-0.4f, 1.0f, 0.64f), new Vector3(0.32f, 0.07f, 0.02f), glow);
            Prim.Box(t, "RearPanel", new Vector3(0f, 0.8f, -1.95f), new Vector3(1.8f, 0.9f, 0.1f), shell);
            foreach (var side in new[] { -1f, 1f })
            {
                Prim.Box(t, "Door", new Vector3(side * 0.92f, 0.7f, -0.45f), new Vector3(0.08f, 0.6f, 2.8f), trim);
                Prim.Box(t, "PillarA", new Vector3(side * 0.88f, 1.2f, 0.5f), new Vector3(0.07f, 0.85f, 0.07f), shell);
                Prim.Box(t, "PillarB", new Vector3(side * 0.9f, 1.25f, -0.6f), new Vector3(0.08f, 0.8f, 0.12f), shell);
                Prim.Box(t, "PillarC", new Vector3(side * 0.85f, 1.25f, -1.85f), new Vector3(0.08f, 0.8f, 0.12f), shell);
            }

            // Seats (tops stay below eye height so looking back is not blocked)
            foreach (var x in new[] { -0.4f, 0.4f })
            {
                Prim.Box(t, "SeatBase", new Vector3(x, 0.36f, -0.15f), new Vector3(0.5f, 0.18f, 0.5f), seat);
                Prim.Box(t, "SeatBack", new Vector3(x, 0.78f, -0.45f), new Vector3(0.5f, 0.62f, 0.12f), seat);
            }

            Prim.Box(t, "RearBase", new Vector3(0f, 0.35f, -1.2f), new Vector3(1.7f, 0.2f, 0.55f), seat);
            Prim.Box(t, "RearBack", new Vector3(0f, 0.85f, -1.58f), new Vector3(1.7f, 0.72f, 0.12f), seat);

            BuildSteeringWheel(t, trim);

            // Interior light: a dim dashboard glow and a cold fill for the back seat.
            Prim.PointLight(t, "DashGlow", new Vector3(0f, 1.05f, 0.7f), new Color(0.35f, 0.85f, 0.65f), 3.4f, 1.1f);
            Prim.PointLight(t, "RearFill", new Vector3(0f, 1.45f, -1.0f), new Color(0.55f, 0.6f, 0.9f), 3.4f, 0.45f);

            // Headlights
            var headGo = new GameObject("Headlights");
            headGo.transform.SetParent(t, false);
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
            var headPivot = new GameObject("DriverHead");
            headPivot.transform.SetParent(t, false);
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
            look.Init(cam, mirror);

            car.Init(route, StartDistance);
            return new CarParts { Car = car, Look = look, MirrorTarget = mirror, Eye = camGo.transform };
        }

        static void BuildSteeringWheel(Transform car, Material mat)
        {
            var wheel = new GameObject("SteeringWheel").transform;
            wheel.SetParent(car, false);
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

        Transform BuildMirror(Transform car)
        {
            const float aspect = 3.2f;
            var housing = Prim.Lit(new Color(0.04f, 0.04f, 0.05f), 0.1f);
            var mirrorPos = new Vector3(0f, 1.42f, 0.8f);

            Prim.Box(car, "MirrorStalk", mirrorPos + new Vector3(0f, 0.1f, 0.03f), new Vector3(0.03f, 0.2f, 0.03f), housing);
            Prim.Box(car, "MirrorHousing", mirrorPos + new Vector3(0f, 0f, 0.012f), new Vector3(0.34f, 0.125f, 0.02f), housing);

            var rt = new RenderTexture(512, Mathf.RoundToInt(512 / aspect), 16) { name = "MirrorRT" };
            var mat = Prim.Unlit(Color.white);
            mat.SetTexture("_BaseMap", rt);
            mat.SetTextureScale("_BaseMap", new Vector2(-1f, 1f)); // a mirror flips the image horizontally
            mat.SetTextureOffset("_BaseMap", new Vector2(1f, 0f));

            var quad = Prim.Make(PrimitiveType.Quad, car, "MirrorGlass", mirrorPos, new Vector3(0.3f, 0.3f / aspect, 1f), mat);

            var camGo = new GameObject("MirrorCamera");
            camGo.transform.SetParent(car, false);
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
            var distances = new[] { 470f, 670f, 880f };
            var hazards = new RoadHazard[distances.Length];

            var skin = Prim.Lit(new Color(0.75f, 0.74f, 0.7f), 0.05f);
            var coat = Prim.Lit(new Color(0.2f, 0.19f, 0.2f), 0.05f);

            for (var i = 0; i < distances.Length; i++)
            {
                var go = new GameObject($"Hazard_{i}");
                go.transform.SetParent(worldRoot, false);
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

        static RideDefinition CreateFallbackRide() => QuietOneFare.Create();
    }
}
