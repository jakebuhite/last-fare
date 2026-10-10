using LastFare.Driving;
using NUnit.Framework;

namespace LastFare.Tests
{
    public class ArcadeDriveTests
    {
        const float Dt = 0.02f;

        static void Run(ArcadeDrive drive, float seconds, float throttle, float brake, bool handbrake = false, float steer = 0f)
        {
            for (var t = 0f; t < seconds; t += Dt) drive.Step(throttle, brake, handbrake, steer, Dt);
        }

        [Test]
        public void Throttle_AcceleratesUpToMaxSpeed()
        {
            var drive = new ArcadeDrive();
            Run(drive, 1f, 1f, 0f);
            Assert.That(drive.Speed, Is.GreaterThan(0f));
            Run(drive, 30f, 1f, 0f);
            Assert.AreEqual(drive.MaxSpeed, drive.Speed, 0.001f);
        }

        [Test]
        public void HoldingBrake_StopsThenReverses()
        {
            var drive = new ArcadeDrive { Speed = 10f };
            Run(drive, 0.5f, 0f, 1f);
            Assert.That(drive.Braking, Is.True);
            Run(drive, 5f, 0f, 1f);
            Assert.AreEqual(-drive.MaxReverseSpeed, drive.Speed, 0.001f);
        }

        [Test]
        public void Coasting_SlowsToAStop()
        {
            var drive = new ArcadeDrive { Speed = 5f };
            Run(drive, 10f, 0f, 0f);
            Assert.AreEqual(0f, drive.Speed, 0.001f);
        }

        [Test]
        public void Handbrake_StopsWithoutReversing()
        {
            var drive = new ArcadeDrive { Speed = 12f };
            Run(drive, 5f, 0f, 0f, handbrake: true);
            Assert.AreEqual(0f, drive.Speed, 0.001f);
        }

        [Test]
        public void SteeringRight_TurnsRightForwardAndLeftInReverse()
        {
            var forward = new ArcadeDrive { Speed = 8f };
            Run(forward, 0.5f, 1f, 0f, steer: 1f);
            Assert.That(forward.Step(1f, 0f, false, 1f, Dt), Is.GreaterThan(0f));

            var reverse = new ArcadeDrive { Speed = -3f };
            Run(reverse, 0.5f, 0f, 1f, steer: 1f);
            Assert.That(reverse.Step(0f, 1f, false, 1f, Dt), Is.LessThan(0f));
        }

        [Test]
        public void Stationary_DoesNotTurn()
        {
            var drive = new ArcadeDrive();
            Run(drive, 1f, 0f, 0f, steer: 1f);
            Assert.AreEqual(0f, drive.Step(0f, 0f, false, 1f, Dt), 0.0001f);
        }
    }
}
