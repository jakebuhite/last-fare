namespace LastFare.Rides
{
    /// <summary>Phases of a single fare. Drives UI, audio and scripted events.</summary>
    public enum RideState
    {
        Idle,       // Waiting for a request.
        Requested,  // Request on the phone; accept or decline.
        EnRoute,    // Driving to the pickup point.
        Boarding,   // Passenger getting in.
        InRide,     // Driving to the dropoff; most scares happen here.
        DroppingOff,
        Completed,
        Failed      // Passenger gone, ride cancelled, or driver did not survive.
    }
}
