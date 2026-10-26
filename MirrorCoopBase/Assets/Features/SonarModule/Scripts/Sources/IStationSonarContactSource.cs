using Features.SonarModule.Contracts;

namespace Features.SonarModule.Scripts.Sources {
    public interface IStationSonarContactSource {
        // The next station while the ship flies; none at a station.
        public bool TryGetStation(out SonarContact station);
    }
}
