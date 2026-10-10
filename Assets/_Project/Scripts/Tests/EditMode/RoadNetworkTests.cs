using LastFare.Driving;
using LastFare.Navigation;
using NUnit.Framework;
using UnityEngine;

namespace LastFare.Tests
{
    public class RoadNetworkTests
    {
        // 3 x 3 intersections, 10 m apart: (0,0) .. (20,20)
        static RoadNetwork Grid() => RoadNetwork.Grid(3, 3, 10f, Vector3.zero);

        [Test]
        public void Grid_HasExpectedIntersectionsAndRoads()
        {
            var net = Grid();
            Assert.AreEqual(9, net.NodeCount);
            Assert.AreEqual(12, net.EdgeCount);
            Assert.AreEqual(new Vector3(20f, 0f, 10f), net.Node(2 + 1 * 3));
        }

        [Test]
        public void ClosestPoint_SnapsOntoNearestRoad()
        {
            var p = Grid().ClosestPoint(new Vector3(4f, 0f, 2f), out _);
            Assert.AreEqual(0f, Vector3.Distance(new Vector3(4f, 0f, 0f), p), 0.001f);
        }

        [Test]
        public void FindRoute_CornerToCorner_IsManhattanDistance()
        {
            var route = Grid().FindRoute(Vector3.zero, new Vector3(20f, 0f, 20f));
            Assert.AreEqual(40f, GpsDirections.Length(route), 0.001f);
            Assert.AreEqual(Vector3.zero, route[0]);
            Assert.AreEqual(new Vector3(20f, 0f, 20f), route[route.Count - 1]);
        }

        [Test]
        public void FindRoute_StartsAndEndsOnTheRoad()
        {
            var route = Grid().FindRoute(new Vector3(5f, 0f, -3f), new Vector3(23f, 0f, 15f));
            Assert.AreEqual(0f, Vector3.Distance(new Vector3(5f, 0f, 0f), route[0]), 0.001f);
            Assert.AreEqual(0f, Vector3.Distance(new Vector3(20f, 0f, 15f), route[route.Count - 1]), 0.001f);
            Assert.AreEqual(5f + 10f + 15f, GpsDirections.Length(route), 0.001f);
        }

        [Test]
        public void FindRoute_TurnAroundPenalty_PrefersCarryingOn()
        {
            var net = Grid();
            var from = new Vector3(4f, 0f, 0f);
            var to = new Vector3(0f, 0f, 20f);

            // Shortest is back west then north (4 + 20); carrying on east is 6 + 20 + 10.
            Assert.AreEqual(24f, GpsDirections.Length(net.FindRoute(from, to, Vector3.right)), 0.001f);
            Assert.AreEqual(36f, GpsDirections.Length(net.FindRoute(from, to, Vector3.right, 15f)), 0.001f);
        }

        [Test]
        public void FindRoute_NeverDoublesBackAlongTheStartingStreet()
        {
            // Near the east end of the bottom street, facing east, heading for the top-left corner.
            var from = new Vector3(9f, 0f, 0f);
            var route = Grid().FindRoute(from, new Vector3(0f, 0f, 20f), Vector3.right, 15f);
            Assert.AreEqual(new Vector3(10f, 0f, 0f), route[1]);
            Assert.AreNotEqual(new Vector3(0f, 0f, 0f), route[2]);
        }

        [Test]
        public void FindRoute_SameRoad_GoesStraightThere()
        {
            var route = Grid().FindRoute(new Vector3(2f, 0f, 0f), new Vector3(8f, 0f, 0f));
            Assert.AreEqual(2, route.Count);
            Assert.AreEqual(6f, GpsDirections.Length(route), 0.001f);
        }
    }
}
