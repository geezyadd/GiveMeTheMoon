using Features.MvpModule;

namespace Features.StagePresetModule.Scripts.UI {
    public abstract class StagePresetLabelViewBase : ViewBehaviour {
        public abstract void ShowPreset(string text);
        public abstract void HidePreset();
    }
}
