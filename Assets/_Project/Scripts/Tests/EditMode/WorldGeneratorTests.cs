using System.Collections.Generic;
using LastFare.Navigation;
using LastFare.World;
using NUnit.Framework;
using UnityEngine;

namespace LastFare.Tests
{
    public class WorldGeneratorTests
    {
        static WorldPlan plan;

        // Generating is deterministic and takes a moment, so share one plan.
        static WorldPlan Plan => plan ??= WorldGenerator.Generate();

        [Test]
        public void Generate_IsDeterministic()
        {
            var again = WorldGenerator.Generate();
            Assert.AreEqual(Plan.Nodes.Count, again.Nodes.Count);
            Assert.AreEqual(Plan.Houses.Count, again.Houses.Count);
            Assert.AreEqual(Plan.Trees.Count, again.Trees.Count);
            Assert.AreEqual(Plan.Houses[17].Address, again.Houses[17].Address);
            Assert.AreEqual(Plan.Houses[17].Position, again.Houses[17].Position);
        }

        [Test]
        public void Generate_HasThreeNeighbourhoodsReachedByHighways()
        {
            Assert.AreEqual(3, Plan.Neighborhoods.Count);
            Assert.AreEqual(3, Plan.Highways.Count);
            foreach (var hood in Plan.Neighborhoods)
                Assert.That(Plan.Houses.Exists(h => Plan.Neighborhoods[h.Neighborhood] == hood), hood.Style.Name);
        }

        [Test]
        public void TheQuietOnesDropoff_Exists()
        {
            Assert.That(Plan.TryFindHouse("114 Aldous Street", out var house));
            Assert.AreEqual("Aldous Park", Plan.Neighborhoods[house.Neighborhood].Style.Name);
            Assert.Less(WorldGenerator.DistanceToTarmac(Plan, WorldPlan.CurbsideStop(house, WorldGenerator.StreetLane)), 0f);
        }

        [Test]
        public void Addresses_AreUnique()
        {
            var seen = new HashSet<string>();
            foreach (var house in Plan.Houses)
                Assert.That(seen.Add(house.Address), $"Duplicate address {house.Address}");
        }

        [Test]
        public void EveryRoad_IsReachableFromDowntown()
        {
            var reached = new bool[Plan.Nodes.Count];
            var queue = new Queue<int>();
            queue.Enqueue(0);
            reached[0] = true;
            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                foreach (var r in Plan.Roads)
                {
                    var other = r.A == n ? r.B : r.B == n ? r.A : -1;
                    if (other < 0 || reached[other]) continue;
                    reached[other] = true;
                    queue.Enqueue(other);
                }
            }

            for (var i = 0; i < reached.Length; i++) Assert.That(reached[i], $"Node {i} at {Plan.Nodes[i]} is cut off");
        }

        [Test]
        public void Houses_SitBackFromEveryRoad()
        {
            foreach (var house in Plan.Houses)
                Assert.GreaterOrEqual(WorldGenerator.DistanceToTarmac(Plan, house.Position), 4f, house.Address);
        }

        [Test]
        public void Houses_DoNotOverlap()
        {
            var houses = Plan.Houses;
            for (var i = 0; i < houses.Count; i++)
            {
                var a = houses[i].Position - houses[i].Front * 5f;
                for (var j = i + 1; j < houses.Count; j++)
                {
                    var b = houses[j].Position - houses[j].Front * 5f;
                    Assert.GreaterOrEqual(Vector3.Distance(a, b), 12f, $"{houses[i].Address} / {houses[j].Address}");
                }
            }
        }

        [Test]
        public void Trees_StayOffTheRoads()
        {
            foreach (var tree in Plan.Trees)
                Assert.GreaterOrEqual(WorldGenerator.DistanceToTarmac(Plan, tree.Position), 3f, tree.Position.ToString());
        }

        [Test]
        public void Start_FacesTowardsThePickup()
        {
            var heading = Plan.StartRotation * Vector3.forward;
            var route = Plan.Network.FindRoute(Plan.StartPosition, Plan.PickupPoint, heading, 60f);
            var first = GpsDirections.Describe(route, Plan.StartPosition, heading);
            Assert.AreNotEqual(Maneuver.UTurn, first.Maneuver);
        }

        [Test]
        public void Pickup_IsOnTheRoad_AndDropoffIsADriveAway()
        {
            Assert.Less(WorldGenerator.DistanceToTarmac(Plan, Plan.PickupPoint), 0f);
            Plan.TryFindHouse("114 Aldous Street", out var house);
            var route = Plan.Network.FindRoute(Plan.PickupPoint, house.Curb);
            Assert.Greater(GpsDirections.Length(route), 1000f);
        }
    }
}
