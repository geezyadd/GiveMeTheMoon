using Features.ShipModule.Scripts;

namespace Features.SonarModule.Scripts.Core {
    public interface ISonarFlightRule {
        public bool IsFlying(ShipRunPhase phase);
    }
}
