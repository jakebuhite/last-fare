namespace LastFare.Rides
{
    /// <summary>How a fare ended. Feeds rating, pay, the journal and (later) scoring.</summary>
    public enum RideOutcome
    {
        None,
        Declined,
        Delivered,      // Dropped off, passenger gone.
        StillInTheCar,  // Dropped off, but he never got out. The app says the ride is over.
        Attacked,       // He reached you.
        Deactivated     // Rating fell below the deactivation threshold.
    }
}
