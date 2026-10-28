#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.FieldAgentModule.Scripts.Debug {
    // Where the debug deck trip is. Never renumber, only append.
    public enum FieldAgentDeckTripStage {
        None = 0,
        FlyingIn = 1,
        WalkingToModule = 2,
        AtModule = 3,
        WalkingBack = 4,
        TakingOff = 5,
        Orbiting = 6,
        Lost = 7,
    }
}
#endif
