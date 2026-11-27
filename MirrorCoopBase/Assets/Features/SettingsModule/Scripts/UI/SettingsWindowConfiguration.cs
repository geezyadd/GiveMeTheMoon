using UnityEngine;

namespace Features.SettingsModule.UI {
    // Tuning of the settings window.
    [CreateAssetMenu(
        fileName = nameof(SettingsWindowConfiguration) + "_Default",
        menuName = "Configurations/SettingsModule/" + nameof(SettingsWindowConfiguration))]
    public sealed class SettingsWindowConfiguration : ScriptableObject {
        [Tooltip("After a window mode or resolution change, how long the player has to keep it before it is reverted.")]
        [SerializeField] private int _displayConfirmSeconds = 15;
        [Tooltip("string.Format pattern of the confirm text; {0} is the seconds left.")]
        [SerializeField] private string _displayConfirmFormat = "Keep these display settings?\nReverting in {0} s";

        public int DisplayConfirmSeconds => _displayConfirmSeconds;
        public string DisplayConfirmFormat => _displayConfirmFormat;
    }
}
