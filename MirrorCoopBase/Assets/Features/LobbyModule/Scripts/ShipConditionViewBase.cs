using System.Collections.Generic;
using Features.MvpModule;

namespace Features.LobbyModule.Scripts {
    public abstract class ShipConditionViewBase : ViewBehaviour {
        public abstract void SetPanelVisible(bool isVisible);
        public abstract void SetHull(string text);
        public abstract void SetBreaches(string text, bool hasBreaches);
        public abstract void SetModules(IReadOnlyList<ShipConditionModuleDisplay> modules, float blinkSeconds, float blinksPerSecond);
    }
}
