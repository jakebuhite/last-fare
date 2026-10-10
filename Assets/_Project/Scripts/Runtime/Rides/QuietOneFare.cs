using LastFare.Data;

namespace LastFare.Rides
{
    /// <summary>Content for the first fare. Also used by the editor tool to create the saved assets.</summary>
    public static class QuietOneFare
    {
        public static PassengerDefinition CreatePassenger() => PassengerDefinition.CreateRuntime(
            "Elias V.",
            5.0f,
            new[]
            {
                "Rated 5.0 by 0 drivers",
                "Pickup: Mercy Hill Cemetery, closed since 9 PM",
                "Trip note from a previous driver: \"He never says anything.\""
            },
            new[]
            {
                "He only moves when you aren't looking at him.",
                "Holding your gaze on him pushes him back.",
                "A quick glance is enough to stop him, but he won't stay back."
            });

        public static RideDefinition Create() => RideDefinition.CreateRuntime(
            CreatePassenger(),
            "Mercy Hill Cemetery, north gate",
            "114 Aldous Street",
            28,
            25f);
    }
}
