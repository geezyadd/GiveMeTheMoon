using Features.ShipModule.Scripts;

namespace Features.SonarModule.Scripts.Core {
    // The sonar works while the ship flies: from takeoff to the end of the landing. At a station the console is the
    // radar module again and the station is right there.
    public sealed class SonarFlightRule : ISonarFlightRule {
        public bool IsFlying(ShipRunPhase phase) =>
            phase == ShipRunPhase.Takeoff || phase == ShipRunPhase.Cruise || phase == ShipRunPhase.Landing;
    }
}
