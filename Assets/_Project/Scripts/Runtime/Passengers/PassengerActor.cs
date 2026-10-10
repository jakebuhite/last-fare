using LastFare.World;
using UnityEngine;

namespace LastFare.Passengers
{
    /// <summary>
    /// The Quiet One in the world: waits at the roadside, gets in, then takes a new seat pose
    /// each time <see cref="CreepModel"/> advances. Pops forward when he advances, drifts back when pushed.
    /// </summary>
    public class PassengerActor : MonoBehaviour
    {
        enum Mode { Hidden, Waiting, Boarding, Seated }

        struct Pose
        {
            public Vector3 Position;
            public float Lean;

            public Pose(float x, float y, float z, float lean)
            {
                Position = new Vector3(x, y, z);
                Lean = lean;
            }
        }

        // Car-local seat poses per creep stage. Car forward is +Z, driver sits at x = -0.4.
        static readonly Pose[] StagePoses =
        {
            new Pose(0.45f, 0.45f, -1.2f, 0f),     // 0: rear right, stiffly upright
            new Pose(0.00f, 0.45f, -1.2f, 0f),     // 1: rear centre
            new Pose(-0.45f, 0.45f, -1.2f, 0f),    // 2: directly behind the driver
            new Pose(-0.05f, 0.60f, -0.9f, 40f),   // 3: leaning between the front seats
            new Pose(-0.55f, 0.62f, -0.55f, 50f),  // 4: at the driver's ear
        };

        const float StandingHipHeight = 0.9f;

        [SerializeField] Transform car;
        [SerializeField] Transform eye;
        [SerializeField] Transform upper;
        [SerializeField] GameObject legs;
        [SerializeField] Transform head;

        Mode mode = Mode.Hidden;
        CreepModel creep;

        Vector3 boardStart;
        float boardTime;
        float boardDuration;

        Pose current;
        float currentYaw;
        bool snapNext;

        public CreepModel Creep => creep;
        public Transform Head => head;
        public bool IsSeated => mode == Mode.Seated;

        /// <summary>Creates the greybox body as children. Called once by the scene builder in the editor.</summary>
        public void Build(Transform carTransform, Transform eyeTransform)
        {
            car = carTransform;
            eye = eyeTransform;

            var skin = Prim.Lit(new Color(0.72f, 0.70f, 0.66f), 0.05f);
            var coat = Prim.Lit(new Color(0.06f, 0.06f, 0.07f), 0.05f);
            var black = Prim.Unlit(Color.black);

            upper = new GameObject("Upper").transform;
            upper.SetParent(transform, false);

            Prim.Box(upper, "Torso", new Vector3(0f, 0.34f, 0f), new Vector3(0.5f, 0.62f, 0.26f), coat);
            Prim.Box(upper, "ArmL", new Vector3(-0.32f, 0.34f, 0f), new Vector3(0.12f, 0.58f, 0.14f), coat);
            Prim.Box(upper, "ArmR", new Vector3(0.32f, 0.34f, 0f), new Vector3(0.12f, 0.58f, 0.14f), coat);
            head = Prim.Sphere(upper, "Head", new Vector3(0f, 0.78f, 0f), Vector3.one * 0.24f, skin).transform;
            Prim.Sphere(upper, "Hair", new Vector3(0f, 0.86f, -0.03f), new Vector3(0.26f, 0.18f, 0.26f), coat);
            Prim.Sphere(head, "EyeL", new Vector3(-0.22f, 0.12f, 0.46f), Vector3.one * 0.2f, black);
            Prim.Sphere(head, "EyeR", new Vector3(0.22f, 0.12f, 0.46f), Vector3.one * 0.2f, black);
            Prim.Box(head, "Mouth", new Vector3(0f, -0.2f, 0.47f), new Vector3(0.45f, 0.04f, 0.05f), black);

            legs = new GameObject("Legs");
            legs.transform.SetParent(transform, false);
            Prim.Box(legs.transform, "LegL", new Vector3(-0.12f, 0.45f, 0f), new Vector3(0.17f, 0.9f, 0.18f), coat);
            Prim.Box(legs.transform, "LegR", new Vector3(0.12f, 0.45f, 0f), new Vector3(0.17f, 0.9f, 0.18f), coat);

            Hide();
        }

        public void Hide()
        {
            mode = Mode.Hidden;
            gameObject.SetActive(false);
        }

        public void ShowWaiting(Vector3 worldPosition, Vector3 lookAt)
        {
            transform.SetParent(null, true);
            gameObject.SetActive(true);
            mode = Mode.Waiting;
            SetStanding(true);
            transform.position = worldPosition;
            var flat = lookAt - worldPosition;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(flat);
            upper.localRotation = Quaternion.identity;
        }

        public void BeginBoarding(float seconds)
        {
            mode = Mode.Boarding;
            boardStart = transform.position;
            boardTime = 0f;
            boardDuration = seconds;
        }

        public void StartCreeping(CreepModel model)
        {
            creep = model;
            creep.StageChanged += OnStageChanged;
        }

        public void Tick(float dt, bool watched)
        {
            if (creep == null || mode != Mode.Seated) return;
            creep.Tick(dt, watched);
        }

        /// <summary>The ride is over but he has not left.</summary>
        public void StayBehind() => ShowSeated(2);

        /// <summary>Place him in the back of the car at a given creep stage without running the ride (previews, endings).</summary>
        public void ShowSeated(int stage) => ApplyPoseInstant(Mathf.Clamp(stage, 0, CreepModel.MaxStage));

        void OnStageChanged(int previous, int next) => snapNext = next > previous;

        void Update()
        {
            switch (mode)
            {
                case Mode.Boarding:
                    UpdateBoarding();
                    break;
                case Mode.Seated:
                    UpdateSeated();
                    break;
            }
        }

        void UpdateBoarding()
        {
            boardTime += Time.deltaTime;
            var t = Mathf.Clamp01(boardTime / boardDuration);
            var seatWorld = car.TransformPoint(StagePoses[0].Position);
            seatWorld.y = Mathf.Lerp(boardStart.y, seatWorld.y, Mathf.SmoothStep(0f, 1f, t));
            var flat = Vector3.Lerp(boardStart, seatWorld, Mathf.SmoothStep(0f, 1f, t));
            transform.position = new Vector3(flat.x, seatWorld.y, flat.z);
            transform.rotation = Quaternion.LookRotation(car.forward);
            SetStanding(t < 0.6f);

            if (t >= 1f)
            {
                transform.SetParent(car, true);
                mode = Mode.Seated;
                ApplyPoseInstant(0);
            }
        }

        void UpdateSeated()
        {
            if (creep == null) return;

            var target = StagePoses[creep.Stage];
            var targetYaw = 0f;

            if (creep.Stage >= CreepModel.MaxStage && eye != null)
            {
                var toEye = car.InverseTransformPoint(eye.position) - target.Position;
                targetYaw = Mathf.Atan2(toEye.x, toEye.z) * Mathf.Rad2Deg;
            }

            if (snapNext)
            {
                current = target;
                currentYaw = targetYaw;
                snapNext = false;
            }
            else
            {
                var k = 1f - Mathf.Exp(-5f * Time.deltaTime);
                current.Position = Vector3.Lerp(current.Position, target.Position, k);
                current.Lean = Mathf.Lerp(current.Lean, target.Lean, k);
                currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, k);
            }

            ApplyCurrent();
        }

        void ApplyPoseInstant(int stage)
        {
            gameObject.SetActive(true);
            transform.SetParent(car, false);
            mode = Mode.Seated;
            SetStanding(false);
            current = StagePoses[stage];
            currentYaw = 0f;
            ApplyCurrent();
        }

        void ApplyCurrent()
        {
            transform.localPosition = current.Position;
            transform.localRotation = Quaternion.Euler(0f, currentYaw, 0f);
            upper.localRotation = Quaternion.Euler(current.Lean, 0f, 0f);
        }

        void SetStanding(bool standing)
        {
            legs.SetActive(standing);
            upper.localPosition = new Vector3(0f, standing ? StandingHipHeight : 0f, 0f);
        }
    }
}
