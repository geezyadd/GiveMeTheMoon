namespace Features.EffectPoolModule.Scripts {
    // An effect that disables its own object when it is over (a particle system's stop action); while it plays it is
    // taken, once it is over a pool can play it again.
    public interface ISelfDisablingEffect {
        public bool IsPlaying { get; }
    }
}
