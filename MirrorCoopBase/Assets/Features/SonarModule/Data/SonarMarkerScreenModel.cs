using System;
using System.Collections.Generic;

namespace Features.SonarModule.Data {
    // The world markers of the crew's marks on this peer's screen, laid out once per frame.
    public sealed class SonarMarkerScreenModel {
        private readonly List<SonarMarkerDisplay> _markers = new();

        public IReadOnlyList<SonarMarkerDisplay> Markers => _markers;

        public event Action OnMarkersChanged;

        public void BeginFrame() =>
            _markers.Clear();

        public void AddMarker(SonarMarkerDisplay marker) =>
            _markers.Add(marker);

        public void CommitFrame() =>
            OnMarkersChanged?.Invoke();
    }
}
