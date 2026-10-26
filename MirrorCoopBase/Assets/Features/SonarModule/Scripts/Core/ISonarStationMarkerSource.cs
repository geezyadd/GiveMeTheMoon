using Features.SonarModule.Contracts;

namespace Features.SonarModule.Scripts.Core {
    public interface ISonarStationMarkerSource {
        public bool TryGetStation(out SonarContact station);
    }
}
