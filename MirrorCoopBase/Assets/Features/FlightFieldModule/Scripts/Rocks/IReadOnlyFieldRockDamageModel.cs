using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Rocks {
    public interface IReadOnlyFieldRockDamageModel {
        public int DamagedCount { get; }

        public float GetDamageTaken(FieldRockId id);
    }
}
