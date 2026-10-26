namespace Features.FlightFieldModule.Scripts.Targets {
    // Server: sets the run's target balloon count scale (the stage preset applier) and puts it back when the run ends.
    public interface IFieldTargetCountScaleWriter {
        public void SetCountScale(float countScale);
        public void Reset();
    }
}
