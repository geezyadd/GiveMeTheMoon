using Features.BugModule.Scripts.Configuration;
using UnityEngine;

namespace Features.BugModule.Scripts.Spawn {
    // The rare random group in flight: none while switched off, else the configured chance, higher while a cargo is
    // on board.
    public sealed class BugRandomSpawnRule : IBugRandomSpawnRule {
        private readonly BugConfiguration _bugConfiguration;

        public BugRandomSpawnRule(BugConfiguration bugConfiguration) =>
            _bugConfiguration = bugConfiguration;

        public bool IsGroupComing(bool isEnabled, float roll, bool hasCargoOnBoard) {
            if (isEnabled == false)
                return false;

            float multiplier = hasCargoOnBoard ? _bugConfiguration.CargoSpawnChanceMultiplier : 1f;
            return roll < Mathf.Clamp01(_bugConfiguration.RandomSpawnChance * multiplier);
        }

        public int CalculateGroupSize(float share) {
            int sizes = _bugConfiguration.GroupMaxSize - _bugConfiguration.GroupMinSize + 1;
            return _bugConfiguration.GroupMinSize + Mathf.Min(sizes - 1, Mathf.FloorToInt(share * sizes));
        }
    }
}
