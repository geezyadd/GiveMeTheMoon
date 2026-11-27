using Features.MvpModule;
using Features.SettingsModule.Data;

namespace Features.SettingsModule.UI {
    // A row of the settings window that shows and edits one setting. Each setting is a prefab variant of a base row
    // prefab that only sets its definition.
    public abstract class SettingElementViewBase : ViewBehaviour {
        public abstract SettingDefinition Definition { get; }
    }
}
