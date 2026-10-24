using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    // How many target balloons one flight gets: a roll in the loop 0 range plus the loop's share, times the stage
    // preset's scale, at most the configuration's cap.
    public sealed class FieldTargetCountRule : IFieldTargetCountRule {
        private readonly FieldTargetConfiguration _fieldTargetConfiguration;

        public FieldTargetCountRule(FieldTargetConfiguration fieldTargetConfiguration) =>
            _fieldTargetConfiguration = fieldTargetConfiguration;

        public int GetCount(int loopIndex, float countScale, IFieldRandom random) {
            Vector2Int firstLoopCount = _fieldTargetConfiguration.FirstLoopCount;
            float count = random.Range(firstLoopCount.x, firstLoopCount.y + 1) + Mathf.Max(0, loopIndex) * _fieldTargetConfiguration.CountPerLoop;
            return Mathf.Clamp(Mathf.RoundToInt(count * Mathf.Max(0f, countScale)), 0, _fieldTargetConfiguration.MaxCount);
        }
    }
}
