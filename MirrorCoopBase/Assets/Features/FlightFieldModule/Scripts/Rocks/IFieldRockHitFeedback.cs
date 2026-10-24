namespace Features.FlightFieldModule.Scripts.Rocks {
    // Every peer: the short flash and shake of a rock a shot strikes.
    public interface IFieldRockHitFeedback {
        public void Play(FieldRockTarget target);
        // Puts the rock back at rest at once (it goes back to the pool).
        public void Stop(FieldRockTarget target);
    }
}
