using Features.FlightFieldModule.Scripts.Generation;

namespace Features.FlightFieldModule.Scripts.Targets {
    public interface IFieldTargetCountRule {
        public int GetCount(int loopIndex, float countScale, IFieldRandom random);
    }
}
