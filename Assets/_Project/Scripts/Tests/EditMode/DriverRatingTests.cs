using LastFare.Player;
using NUnit.Framework;

namespace LastFare.Tests
{
    public class DriverRatingTests
    {
        [Test]
        public void Apply_ClampsToMax()
        {
            var rating = new DriverRating(4.9f);
            rating.Apply(1f);
            Assert.AreEqual(DriverRating.Max, rating.Value, 0.0001f);
        }

        [Test]
        public void Apply_BelowThreshold_DeactivatesOnce()
        {
            var rating = new DriverRating(4f, 3.5f);
            var deactivations = 0;
            rating.Deactivated += () => deactivations++;

            rating.Apply(-1f);
            rating.Apply(-1f);

            Assert.IsTrue(rating.IsDeactivated);
            Assert.AreEqual(1, deactivations);
        }
    }
}
