using System.Collections.Generic;
using Features.MvpModule;
using Features.SonarModule.Data;

namespace Features.SonarModule.Scripts.UI {
    public abstract class SonarMarkersViewBase : ViewBehaviour {
        public abstract void ShowMarkers(IReadOnlyList<SonarMarkerDisplay> markers);
    }
}
