#if UNITY_INCLUDE_TESTS
namespace Tests.PlayMode.LoopSmoke {
    // Where in the loop test a registered step runs. InLobby and AfterRun belong to a run, the flight phases to a
    // flight of a run, InMenu to the end of the test after Leave.
    public enum LoopSmokePhase {
        None = 0,
        // In the lobby before the run's Start.
        InLobby = 1,
        // At the station before the flight's launch: after the engines and the boarding at the run's first station.
        BeforeTakeoff = 2,
        InCruise = 3,
        // At the station after the flight's landing checks.
        AfterLanding = 4,
        // Back in the lobby after the run ended.
        AfterRun = 5,
        // In the menu after the last run and Leave.
        InMenu = 6
    }
}
#endif
