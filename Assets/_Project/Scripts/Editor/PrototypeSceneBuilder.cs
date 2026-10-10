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
    /// Builds the greybox prototype into the active scene as ordinary GameObjects: the world from
    /// <see cref="WorldGenerator"/> (downtown, highways, neighbourhoods), the drivable car with a working
    /// rear-view mirror, the passenger, hazards, GPS, audio, HUD and the FareDirector, all wired through
    /// serialized references. Generated materials, meshes and prefabs go to <see cref="GeneratedAssets.Dir"/>.
    /// </summary>
    public class PrototypeSceneBuilder
    {
        const int Seed = 1234;

        readonly RideDefinition ride;
        readonly GeneratedAssets assets = new();

        public PrototypeSceneBuilder(RideDefinition ride, Material litMaterial, Material unlitMaterial)
        {
            this.ride = ride;
            Prim.Init(litMaterial, unlitMaterial);
        }

        /// <summary>Builds everything into the active scene. Call <see cref="GeneratedAssets.ResetFolders"/> first.</summary>
        public void Build()
        {
            var rng = new System.Random(Seed);
            var plan = WorldGenerator.Generate(Seed);
            if (!plan.TryFindHouse(ride.DropoffLabel, out var dropoffHouse))
                throw new System.InvalidOperationException($"The world has no house at '{ride.DropoffLabel}'.");

            SetupAtmosphere();

            var roadMap = new GameObject("RoadMap").AddComponent<RoadMap>();
            var radii = plan.NodeRadius.ToArray();
            roadMap.SetData(plan.Nodes.ToArray(), radii, plan.Roads.ToArray(), plan.Labels.ToArray());
            EditorUtility.SetDirty(roadMap);

            var prefabs = WorldPrefabs.Create(assets, rng);
            new WorldDresser(plan, prefabs, rng, dropoffHouse.Address).Build();

            // Stops: the cemetery gate downtown, and the passenger's house out in the suburbs.
            var stops = new GameObject("Stops").transform;
            var pickupPoint = Point(stops, "PickupPoint", plan.PickupPoint);
            var standPoint = Point(stops, "PickupStandPoint", plan.PickupStandPoint);
            var lookPoint = Point(stops, "PickupLookPoint", plan.PickupLookPoint);
            var pickupMarker = BuildMarker(stops, "PickupMarker", new Vector3(plan.PickupPoint.x, 0f, plan.PickupStandPoint.z + 1f), new Color(0.45f, 0.85f, 1f));
            var dropoffPoint = Point(stops, "DropoffPoint", WorldPlan.CurbsideStop(dropoffHouse, WorldGenerator.StreetLane));
            var dropoffMarker = BuildMarker(stops, "DropoffMarker",
                dropoffHouse.Curb - dropoffHouse.Front * (WorldGenerator.StreetWidth * 0.5f + 0.6f), new Color(0.55f, 1f, 0.6f));

            var carParts = BuildCar(plan.StartPosition, plan.StartRotation, roadMap);
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

            Wire(gps, ("map", roadMap), ("vehicle", carParts.Car.transform));
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

        /// <summary>Saves any generated materials and meshes left in the scene (call after <see cref="Build"/>).</summary>
        public void SaveAssets(GameObject[] roots) => assets.Save(roots);

        // ------------------------------------------------------------ Atmosphere

        static void SetupAtmosphere()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.045f, 0.075f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.02f;
            RenderSettings.fogColor = new Color(0.03f, 0.035f, 0.05f);

            // No sky to reflect: without this, glossy asphalt mirrors Unity's default grey sky and glows.
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.reflectionIntensity = 0.3f;

            var moon = new GameObject("Moonlight").AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.45f, 0.55f, 0.85f);
            moon.intensity = 0.12f;
            moon.shadows = LightShadows.None;
            moon.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
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

        CarParts BuildCar(Vector3 position, Quaternion rotation, RoadMap roads)
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
            Wire(car, ("roads", roads));

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

            var wheel = BuildSteeringWheel(t, trim);
            Wire(wheel.gameObject.AddComponent<SteeringWheel>(), ("car", car));

            // Interior light: a dim dashboard glow and a cold fill for the back seat.
            var lights = Child(t, "Lights").transform;
            Prim.PointLight(lights, "DashGlow", new Vector3(0f, 1.05f, 0.7f), new Color(0.35f, 0.85f, 0.65f), 3.4f, 1.1f);
            Prim.PointLight(lights, "RearFill", new Vector3(0f, 1.45f, -1.0f), new Color(0.55f, 0.6f, 0.9f), 3.4f, 0.45f);

            // Headlights: long enough to see a figure on the highway in time, if you're paying attention.
            var headGo = Child(lights, "Headlights");
            headGo.transform.localPosition = new Vector3(0f, 0.8f, 2.7f);
            headGo.transform.localRotation = Quaternion.Euler(3f, 0f, 0f);
            var head = headGo.AddComponent<Light>();
            head.type = LightType.Spot;
            head.range = 70f;
            head.spotAngle = 75f;
            head.innerSpotAngle = 35f;
            head.intensity = 90f;
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
            cam.farClipPlane = 260f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            camGo.AddComponent<AudioListener>();

            var mirror = BuildMirror(t);
            Wire(look, ("cam", cam), ("mirrorTarget", mirror));

            return new CarParts { Car = car, Look = look, Eye = camGo.transform };
        }

        static Transform BuildSteeringWheel(Transform car, Material mat)
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
            return wheel;
        }

        Transform BuildMirror(Transform car)
        {
            const float aspect = 3.2f;
            var root = Child(car, "RearViewMirror").transform;
            var housing = Prim.Lit(new Color(0.04f, 0.04f, 0.05f), 0.1f);
            var mirrorPos = new Vector3(0f, 1.42f, 0.8f);

            Prim.Box(root, "MirrorStalk", mirrorPos + new Vector3(0f, 0.1f, 0.03f), new Vector3(0.03f, 0.2f, 0.03f), housing);
            Prim.Box(root, "MirrorHousing", mirrorPos + new Vector3(0f, 0f, 0.012f), new Vector3(0.34f, 0.125f, 0.02f), housing);

            var rt = new RenderTexture(512, Mathf.RoundToInt(512 / aspect), 16) { name = "MirrorRT" };
            assets.SaveAsset(rt, "MirrorRT.renderTexture");

            var mat = Prim.Unlit(Color.white);
            mat.name = "Mirror";
            mat.SetTexture("_BaseMap", rt);
            mat.SetTextureScale("_BaseMap", new Vector2(-1f, 1f)); // a mirror flips the image horizontally
            mat.SetTextureOffset("_BaseMap", new Vector2(1f, 0f));
            assets.SaveAsset(mat, "Mirror.mat");

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
    }
}
