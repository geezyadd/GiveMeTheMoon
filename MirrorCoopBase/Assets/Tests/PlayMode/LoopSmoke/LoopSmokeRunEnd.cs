#if UNITY_INCLUDE_TESTS
namespace Tests.PlayMode.LoopSmoke {
    // How a run of the loop test goes back to the lobby after its last flight.
    public enum LoopSmokeRunEnd {
        None = 0,
        AllDead = 1,
        HullLoss = 2,
        // The run's last flight never lands: the hull is destroyed in its cruise with the crew on the deck.
        HullLossInCruise = 3
    }
}
#endif
