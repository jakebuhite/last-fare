using UnityEngine;

namespace LastFare.Data
{
    /// <summary>One bookable ride: who, from where, to where, for how much.</summary>
    [CreateAssetMenu(menuName = "Last Fare/Ride", fileName = "Ride_")]
    public class RideDefinition : ScriptableObject
    {
        [SerializeField] PassengerDefinition passenger;
        [SerializeField] string pickupLabel;
        [SerializeField] string dropoffLabel;
        [Min(0)] [SerializeField] int fare = 12;

        [Tooltip("Seconds the request stays on screen before it expires.")]
        [Min(1f)] [SerializeField] float requestWindow = 15f;

        public PassengerDefinition Passenger => passenger;
        public string PickupLabel => pickupLabel;
        public string DropoffLabel => dropoffLabel;
        public int Fare => fare;
        public float RequestWindow => requestWindow;
    }
}
