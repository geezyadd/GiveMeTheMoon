using Features.SonarModule.Contracts;
using Features.SonarModule.Data;
using Features.SonarModule.Scripts.Sources;

namespace Features.SonarModule.Scripts.Core {
    // The station this peer's screen marks on its own: only while the marker is turned on and the sonar has the
    // station, which it has only while the ship flies.
    public sealed class SonarStationMarkerSource : ISonarStationMarkerSource {
        private readonly SonarStationMarkerModel _sonarStationMarkerModel;
        private readonly IStationSonarContactSource _stationSonarContactSource;

        public SonarStationMarkerSource(SonarStationMarkerModel sonarStationMarkerModel, IStationSonarContactSource stationSonarContactSource) {
            _sonarStationMarkerModel = sonarStationMarkerModel;
            _stationSonarContactSource = stationSonarContactSource;
        }

        public bool TryGetStation(out SonarContact station) {
            station = default;
            return _sonarStationMarkerModel.IsShown && _stationSonarContactSource.TryGetStation(out station);
        }
    }
}
