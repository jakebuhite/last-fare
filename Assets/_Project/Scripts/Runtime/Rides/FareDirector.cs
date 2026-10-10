using System.Collections;
using LastFare.Audio;
using LastFare.Data;
using LastFare.Driving;
using LastFare.Passengers;
using LastFare.Player;
using LastFare.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LastFare.Rides
{
    /// <summary>
    /// Runs one complete fare: offer > accept > drive to pickup > board > ride (The Quiet One creeps,
    /// road hazards demand attention) > drop-off > result. Reacts to <see cref="RideController"/> state
    /// changes; the HUD, audio and passenger read from here and never drive the flow themselves.
    /// </summary>
    public class FareDirector : MonoBehaviour
    {
        // Tuning
        const float FirstOfferDelay = 3f;
        const float ReofferDelay = 4f;
        const float BoardingSeconds = 3f;
        const float DropoffSeconds = 3.5f;
        const float WatchAngleDirect = 28f;   // looking straight at him
        const float DeliveredRating = 0.05f;
        const float StillInCarRating = -0.3f;
        const float AttackedRating = -1.6f;
        const float HazardRating = -0.4f;

        [Header("Content")]
        [SerializeField] RideDefinition ride;

        [Header("Scene")]
        [SerializeField] RideController controller;
        [SerializeField] CarController car;
        [SerializeField] DriverLook look;
        [SerializeField] PassengerActor passenger;
        [SerializeField] LastFareAudio sound;
        [SerializeField] HudView hud;
        [SerializeField] RoadHazard[] hazards;
        [SerializeField] GameObject pickupMarker;
        [SerializeField] GameObject dropoffMarker;
        [SerializeField] Transform pickupStandPoint;
        [SerializeField] Transform pickupLookPoint;

        [Header("Stops (metres along the route)")]
        [SerializeField] float pickupDistance = 250f;
        [SerializeField] float dropoffDistance = 1050f;

        float idleTimer;
        float offerDelay = FirstOfferDelay;
        float requestTimer;
        float stateTimer;
        bool finished;

        public DriverRating Rating { get; private set; }
        public int Earnings { get; private set; }
        public RideOutcome Outcome { get; private set; }
        public RideDefinition Ride => ride;
        public RideController Controller => controller;
        public string Objective { get; private set; } = "";
        public float RequestTimeLeft01 => ride == null ? 0f : Mathf.Clamp01(requestTimer / ride.RequestWindow);
        public float Fade { get; private set; }   // 0 clear .. 1 black
        public float Flash { get; private set; }  // 0 clear .. 1 white
        public bool ShowResult { get; private set; }
        public string ResultTitle { get; private set; } = "";
        public string ResultBody { get; private set; } = "";
        public float RatingChange { get; private set; }
        public int FarePaid { get; private set; }

        void Awake()
        {
            if (ride == null) ride = QuietOneFare.Create();
            if (controller == null) controller = GetComponent<RideController>();

            Rating = new DriverRating();
            Rating.Deactivated += OnDeactivated;

            controller.StateChanged += OnStateChanged;

            foreach (var hazard in hazards)
            {
                hazard.Hit += OnHazardHit;
                hazard.Cleared += OnHazardCleared;
            }
        }

        void Start()
        {
            pickupMarker.SetActive(false);
            dropoffMarker.SetActive(false);
            Objective = "Waiting for a ride request...";
            hud.Caption("Night shift. Rating 4.9. Keep it that way.", 5f);
        }

        // ---------------------------------------------------------------- Update

        void Update()
        {
            if (controller == null) return;

            sound.SetEngine(car.Speed, car.MaxSpeed);
            UpdateOverlays();

            if (ShowResult)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0
                        ? SceneManager.GetActiveScene().buildIndex
                        : 0);
                return;
            }

            if (finished) return;

            var kb = Keyboard.current;
            switch (controller.State)
            {
                case RideState.Idle:
                    idleTimer += Time.deltaTime;
                    if (idleTimer >= offerDelay) controller.Offer(ride);
                    break;

                case RideState.Requested:
                    requestTimer -= Time.deltaTime;
                    if (kb != null && kb.eKey.wasPressedThisFrame) controller.Accept();
                    else if ((kb != null && kb.qKey.wasPressedThisFrame) || requestTimer <= 0f) controller.Decline();
                    break;

                case RideState.EnRoute:
                    Objective = $"Drive to pickup: {ride.PickupLabel}  ({Mathf.Max(0f, car.RemainingToStop):0} m)";
                    if (car.IsStoppedAtTarget) controller.ArrivedAtPickup();
                    break;

                case RideState.Boarding:
                    Objective = "Passenger boarding...";
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f) controller.PassengerSeated();
                    break;

                case RideState.InRide:
                    Objective = $"Drop off: {ride.DropoffLabel}  ({Mathf.Max(0f, car.RemainingToStop):0} m)";
                    TickPassenger();
                    if (!finished && car.IsStoppedAtTarget) controller.ArrivedAtDropoff();
                    break;

                case RideState.DroppingOff:
                    Objective = "Drop-off";
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f) controller.Complete();
                    break;
            }
        }

        void TickPassenger()
        {
            var creep = passenger.Creep;
            var watched = IsWatchingPassenger();
            passenger.Tick(Time.deltaTime, watched);
            sound.SetTension(creep.Stage / (float)CreepModel.MaxStage);

            if (creep.HasReachedPlayer) StartCoroutine(AttackSequence());
        }

        bool IsWatchingPassenger()
        {
            // Looking straight at him (turned around), or holding the mirror.
            if (look.HoldingMirror) return true;
            return look.AngleTo(passenger.Head.position) < WatchAngleDirect;
        }

        // ------------------------------------------------------------- Transitions

        void OnStateChanged(RideState previous, RideState next)
        {
            switch (next)
            {
                case RideState.Idle:
                    idleTimer = 0f;
                    offerDelay = ReofferDelay;
                    Objective = "Waiting for a ride request...";
                    break;

                case RideState.Requested:
                    requestTimer = ride.RequestWindow;
                    sound.PlayBump();
                    break;

                case RideState.EnRoute:
                    car.StopAt(pickupDistance);
                    pickupMarker.SetActive(true);
                    passenger.ShowWaiting(pickupStandPoint.position, pickupLookPoint.position);
                    hud.Caption($"Pickup in {ride.PickupLabel}. Someone is already waiting.", 5f);
                    break;

                case RideState.Boarding:
                    pickupMarker.SetActive(false);
                    passenger.BeginBoarding(BoardingSeconds - 0.5f);
                    sound.PlayDoor();
                    stateTimer = BoardingSeconds;
                    hud.Caption("The door opens. Nobody says hello.", 4f);
                    break;

                case RideState.InRide:
                    sound.PlayDoor();
                    passenger.StartCreeping(new CreepModel());
                    car.StopAt(dropoffDistance);
                    dropoffMarker.SetActive(true);
                    foreach (var hazard in hazards) hazard.Arm(car);
                    hud.Caption("Rule of thumb: he only moves when you aren't looking.", 6f);
                    break;

                case RideState.DroppingOff:
                    dropoffMarker.SetActive(false);
                    foreach (var hazard in hazards) hazard.Disarm();
                    sound.SetTension(0f);
                    stateTimer = DropoffSeconds;
                    DecideDropoffOutcome();
                    break;

                case RideState.Completed:
                    FinishRide();
                    break;
            }
        }

        void DecideDropoffOutcome()
        {
            if (passenger.Creep.HighestStage >= 3)
            {
                Outcome = RideOutcome.StillInTheCar;
                passenger.StayBehind();
                hud.Caption("\"Ride complete.\" He's still in the back seat.", 5f);
            }
            else
            {
                Outcome = RideOutcome.Delivered;
                passenger.Hide();
                hud.Caption("He gets out without a word. The door closes by itself.", 5f);
            }

            sound.PlayDoor();
        }

        void FinishRide()
        {
            FarePaid = ride.Fare;
            Earnings += FarePaid;

            var before = Rating.Value;
            Rating.Apply(Outcome == RideOutcome.Delivered ? DeliveredRating : StillInCarRating);
            RatingChange = Rating.Value - before;

            if (Outcome == RideOutcome.Delivered)
            {
                ResultTitle = "RIDE COMPLETE";
                ResultBody = "He's gone. You think.\nThe app thanks you for driving.";
            }
            else
            {
                ResultTitle = "RIDE COMPLETE?";
                ResultBody = "The app says he left.\nYour mirror says otherwise.";
            }

            finished = true;
            StartCoroutine(ShowResultAfter(1.5f));
        }

        IEnumerator AttackSequence()
        {
            if (finished) yield break;
            finished = true;
            Outcome = RideOutcome.Attacked;

            car.HaltNow();
            foreach (var hazard in hazards) hazard.Disarm();
            look.InputEnabled = false;
            look.LockOn(passenger.Head);
            look.Shake(0.12f, 2f);
            sound.SetTension(1f);
            sound.PlayStinger();
            Flash = 1f;
            hud.Caption("", 0f);

            yield return new WaitForSeconds(2.2f);

            var before = Rating.Value;
            Rating.Apply(AttackedRating);
            RatingChange = Rating.Value - before;
            FarePaid = 0;
            ResultTitle = "RIDE CANCELLED";
            ResultBody = "1-star review: \"Driver stopped paying attention.\"";

            StartCoroutine(ShowResultAfter(1.5f));
        }

        IEnumerator ShowResultAfter(float seconds)
        {
            var t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                Fade = Mathf.Clamp01(t / seconds) * (Outcome == RideOutcome.Attacked ? 1f : 0.75f);
                yield return null;
            }

            if (Rating.IsDeactivated)
                ResultBody += "\n\nYour account has been DEACTIVATED.";

            ShowResult = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // ----------------------------------------------------------------- Events

        void OnHazardHit()
        {
            if (finished) return;
            Rating.Apply(HazardRating);
            sound.PlayBump();
            look.Shake(0.05f, 0.6f);
            hud.Caption("Something thumps off the bumper. The road is empty.", 4f);
        }

        void OnHazardCleared()
        {
            hud.Caption("It steps aside. You don't watch where it goes.", 4f);
        }

        void OnDeactivated()
        {
            // The final attack handles its own result; any other drop below threshold ends the night.
            if (finished) return;
            finished = true;
            Outcome = RideOutcome.Deactivated;
            car.HaltNow();
            ResultTitle = "DEACTIVATED";
            ResultBody = "Your rating fell too low.\nYour account has been DEACTIVATED.";
            StartCoroutine(ShowResultAfter(1.5f));
        }

        void UpdateOverlays()
        {
            if (Flash > 0f) Flash = Mathf.MoveTowards(Flash, 0f, Time.deltaTime * 2.5f);
        }
    }
}
