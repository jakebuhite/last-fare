using LastFare.Navigation;
using NUnit.Framework;
using UnityEngine;

namespace LastFare.Tests
{
    public class GpsDirectionsTests
    {
        static readonly Vector3[] NorthThenEast =
        {
            new(0f, 0f, 0f),
            new(0f, 0f, 50f),
            new(30f, 0f, 50f),
        };

        [Test]
        public void Describe_StraightRoute_AnnouncesArrival()
        {
            var route = new[] { Vector3.zero, new Vector3(0f, 0f, 80f) };
            var i = GpsDirections.Describe(route, Vector3.zero, Vector3.forward);
            Assert.AreEqual(Maneuver.Arrive, i.Maneuver);
            Assert.AreEqual(80f, i.RemainingMeters, 0.001f);
        }

        [Test]
        public void Describe_NorthThenEast_IsRightTurnAtTheCorner()
        {
            var i = GpsDirections.Describe(NorthThenEast, Vector3.zero, Vector3.forward);
            Assert.AreEqual(Maneuver.TurnRight, i.Maneuver);
            Assert.AreEqual(50f, i.DistanceToManeuver, 0.001f);
            Assert.AreEqual(80f, i.RemainingMeters, 0.001f);
        }

        [Test]
        public void Describe_NorthThenWest_IsLeftTurn()
        {
            var route = new[] { Vector3.zero, new Vector3(0f, 0f, 50f), new Vector3(-30f, 0f, 50f) };
            Assert.AreEqual(Maneuver.TurnLeft, GpsDirections.Describe(route, Vector3.zero, Vector3.forward).Maneuver);
        }

        [Test]
        public void Describe_FacingAwayFromRoute_AsksForUTurn()
        {
            var i = GpsDirections.Describe(NorthThenEast, Vector3.zero, Vector3.back);
            Assert.AreEqual(Maneuver.UTurn, i.Maneuver);
        }

        [Test]
        public void Describe_CountsDistanceFromCarToRouteStart()
        {
            var i = GpsDirections.Describe(NorthThenEast, new Vector3(3f, 0f, 0f), Vector3.forward);
            Assert.AreEqual(53f, i.DistanceToManeuver, 0.001f);
        }

        [Test]
        public void Describe_NamesTheRoadOfTheTurn()
        {
            var names = new[] { "Mill Street", "Pike Avenue" };
            var i = GpsDirections.Describe(NorthThenEast, Vector3.zero, Vector3.forward, names);
            Assert.AreEqual(Maneuver.TurnRight, i.Maneuver);
            Assert.AreEqual("Pike Avenue", i.Onto);
        }

        [Test]
        public void Describe_StraightOnToANewRoad_SaysContinue()
        {
            var route = new[] { Vector3.zero, new Vector3(0f, 0f, 40f), new Vector3(2f, 0f, 90f) };
            var i = GpsDirections.Describe(route, Vector3.zero, Vector3.forward, new[] { "Route 12", "Aldous Park Drive" });
            Assert.AreEqual(Maneuver.Continue, i.Maneuver);
            Assert.AreEqual("Aldous Park Drive", i.Onto);
            Assert.AreEqual(40f, i.DistanceToManeuver, 0.001f);
        }

        [Test]
        public void FormatDistance_SwitchesToKilometres()
        {
            Assert.AreEqual("850 m", GpsDirections.FormatDistance(847f));
            Assert.AreEqual("1.2 km", GpsDirections.FormatDistance(1234f));
        }

        [Test]
        public void PointAlong_WalksPastCorners()
        {
            var p = GpsDirections.PointAlong(NorthThenEast, 60f, out var dir);
            Assert.AreEqual(0f, Vector3.Distance(new Vector3(10f, 0f, 50f), p), 0.001f);
            Assert.AreEqual(0f, Vector3.Distance(Vector3.right, dir), 0.001f);
        }
    }
}
