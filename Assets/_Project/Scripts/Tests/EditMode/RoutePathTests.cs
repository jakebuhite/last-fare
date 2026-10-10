using LastFare.Driving;
using NUnit.Framework;
using UnityEngine;

namespace LastFare.Tests
{
    public class RoutePathTests
    {
        [Test]
        public void Evaluate_WrapsAroundLoop()
        {
            var route = RoutePath.CreateLoop(100f);
            route.Evaluate(5f, out var a, out _);
            route.Evaluate(5f + route.Length, out var b, out _);
            Assert.AreEqual(0f, Vector3.Distance(a, b), 0.01f);
        }

        [Test]
        public void Evaluate_ForwardIsUnitLengthAndFlat()
        {
            var route = RoutePath.CreateLoop(100f);
            for (var d = 0f; d < route.Length; d += 37f)
            {
                route.Evaluate(d, out _, out var fwd);
                Assert.AreEqual(1f, fwd.magnitude, 0.001f);
                Assert.AreEqual(0f, fwd.y, 0.0001f);
            }
        }

        [Test]
        public void Length_MatchesSquareLoop()
        {
            var route = new RoutePath(new[]
            {
                new Vector3(0, 0, 0), new Vector3(10, 0, 0), new Vector3(10, 0, 10), new Vector3(0, 0, 10)
            });
            Assert.AreEqual(40f, route.Length, 0.001f);
        }
    }
}
