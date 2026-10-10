using System;

namespace LastFare.Passengers
{
    /// <summary>
    /// The Quiet One's behaviour: he only advances while unwatched. Looking at him
    /// resets his advance timer; staring long enough pushes him back a stage.
    /// Stage <see cref="MaxStage"/> means he has reached you. Plain C# for testing.
    /// </summary>
    public class CreepModel
    {
        public const int MaxStage = 4;

        readonly float creepInterval;
        readonly float retreatTime;
        float creepTimer;
        float watchTimer;

        public int Stage { get; private set; }
        public int HighestStage { get; private set; }
        public bool EverWatched { get; private set; }
        public bool HasReachedPlayer => Stage >= MaxStage;

        /// <summary>(previousStage, newStage)</summary>
        public event Action<int, int> StageChanged;

        public CreepModel(float creepInterval = 7f, float retreatTime = 3f)
        {
            this.creepInterval = creepInterval;
            this.retreatTime = retreatTime;
        }

        /// <summary>Seconds of being unwatched needed for the next advance. Gets shorter as he gets closer.</summary>
        public float CurrentInterval => creepInterval * (1f - 0.15f * Stage);

        public void Tick(float deltaTime, bool watched)
        {
            if (HasReachedPlayer) return;

            if (watched)
            {
                EverWatched = true;
                creepTimer = 0f;
                watchTimer += deltaTime;
                if (watchTimer >= retreatTime && Stage > 0)
                {
                    watchTimer = 0f;
                    SetStage(Stage - 1);
                }
            }
            else
            {
                watchTimer = 0f;
                creepTimer += deltaTime;
                if (creepTimer >= CurrentInterval)
                {
                    creepTimer = 0f;
                    SetStage(Stage + 1);
                }
            }
        }

        void SetStage(int next)
        {
            var previous = Stage;
            Stage = next;
            if (Stage > HighestStage) HighestStage = Stage;
            StageChanged?.Invoke(previous, next);
        }
    }
}
