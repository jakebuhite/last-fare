using System;
using LastFare.Data;
using UnityEngine;

namespace LastFare.Rides
{
    /// <summary>
    /// Runs one ride through its states. Other systems (UI, audio, passenger
    /// behaviour) subscribe to <see cref="StateChanged"/> rather than being called from here.
    /// </summary>
    public class RideController : MonoBehaviour
    {
        public RideDefinition Current { get; private set; }
        public RideState State { get; private set; } = RideState.Idle;

        public event Action<RideState, RideState> StateChanged; // (previous, next)

        public void Offer(RideDefinition ride)
        {
            if (State != RideState.Idle)
            {
                Debug.LogWarning($"Ride offered while in state {State}; ignoring.", this);
                return;
            }

            Current = ride;
            SetState(RideState.Requested);
        }

        public void Accept() => Advance(RideState.Requested, RideState.EnRoute);
        public void Decline() => Advance(RideState.Requested, RideState.Idle, clearRide: true);
        public void ArrivedAtPickup() => Advance(RideState.EnRoute, RideState.Boarding);
        public void PassengerSeated() => Advance(RideState.Boarding, RideState.InRide);
        public void ArrivedAtDropoff() => Advance(RideState.InRide, RideState.DroppingOff);
        public void Complete() => Advance(RideState.DroppingOff, RideState.Completed);

        public void Fail()
        {
            if (State is RideState.Idle or RideState.Completed or RideState.Failed) return;
            SetState(RideState.Failed);
        }

        /// <summary>Return to Idle after a ride has ended so the next one can be offered.</summary>
        public void Reset()
        {
            Current = null;
            SetState(RideState.Idle);
        }

        void Advance(RideState from, RideState to, bool clearRide = false)
        {
            if (State != from)
            {
                Debug.LogWarning($"Cannot move to {to}: expected {from} but was {State}.", this);
                return;
            }

            if (clearRide) Current = null;
            SetState(to);
        }

        void SetState(RideState next)
        {
            var previous = State;
            State = next;
            StateChanged?.Invoke(previous, next);
        }
    }
}
