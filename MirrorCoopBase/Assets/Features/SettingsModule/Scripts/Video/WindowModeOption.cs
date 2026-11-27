using System;
using UnityEngine;

namespace Features.SettingsModule.Video {
    // A window mode option of the video settings and the engine's mode it means.
    [Serializable]
    public sealed class WindowModeOption {
        [SerializeField] private string _optionId;
        [SerializeField] private FullScreenMode _mode;

        public string OptionId => _optionId;
        public FullScreenMode Mode => _mode;
    }
}
