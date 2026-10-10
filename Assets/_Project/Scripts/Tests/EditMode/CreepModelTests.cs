using LastFare.Passengers;
using NUnit.Framework;

namespace LastFare.Tests
{
    public class CreepModelTests
    {
        [Test]
        public void Unwatched_AdvancesAfterInterval()
        {
            var model = new CreepModel(creepInterval: 5f);
            model.Tick(4.9f, watched: false);
            Assert.AreEqual(0, model.Stage);
            model.Tick(0.2f, watched: false);
            Assert.AreEqual(1, model.Stage);
        }

        [Test]
        public void QuickGlances_ResetTheTimer()
        {
            var model = new CreepModel(creepInterval: 5f);
            for (var i = 0; i < 10; i++)
            {
                model.Tick(4f, watched: false);
                model.Tick(0.1f, watched: true);
            }

            Assert.AreEqual(0, model.Stage);
            Assert.IsTrue(model.EverWatched);
        }

        [Test]
        public void Staring_PushesHimBack()
        {
            var model = new CreepModel(creepInterval: 1f, retreatTime: 2f);
            model.Tick(1.1f, watched: false);
            Assert.AreEqual(1, model.Stage);

            model.Tick(2.1f, watched: true);
            Assert.AreEqual(0, model.Stage);
            Assert.AreEqual(1, model.HighestStage);
        }

        [Test]
        public void ReachingMaxStage_StopsTicking()
        {
            var model = new CreepModel(creepInterval: 1f);
            for (var i = 0; i < 50; i++) model.Tick(1f, watched: false);

            Assert.IsTrue(model.HasReachedPlayer);
            model.Tick(100f, watched: true);
            Assert.AreEqual(CreepModel.MaxStage, model.Stage);
        }
    }
}
