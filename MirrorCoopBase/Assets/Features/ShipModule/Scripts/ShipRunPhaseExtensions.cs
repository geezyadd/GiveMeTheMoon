namespace Features.ShipModule.Scripts {
    public static class ShipRunPhaseExtensions {
        // The ship flies from the takeoff to the end of the landing: riders ride it, the sonar works.
        public static bool IsFlight(this ShipRunPhase phase) =>
            phase == ShipRunPhase.Takeoff || phase == ShipRunPhase.Cruise || phase == ShipRunPhase.Landing;
    }
}
