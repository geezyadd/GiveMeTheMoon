using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Server: the run's scale of the target balloon count, set by the stage preset applier from the preset's
    // FieldTargetPresetBlock and back to 1 when the run ends. Every flight's field request reads it.
    public sealed class FieldTargetCountScaleModel : IReadOnlyFieldTargetCountScaleModel {
        public float CountScale { get; private set; } = FieldRequest.NO_TARGET_COUNT_SCALE;

        public void SetCountScale(float countScale) =>
            CountScale = countScale;

        public void Reset() =>
            CountScale = FieldRequest.NO_TARGET_COUNT_SCALE;
    }
}
