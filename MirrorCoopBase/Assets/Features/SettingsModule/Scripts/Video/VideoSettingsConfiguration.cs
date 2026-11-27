using System.Collections.Generic;
using UnityEngine;

namespace Features.SettingsModule.Video {
    // What the video settings' option ids mean for the engine: window modes, MSAA samples, shadow levels.
    [CreateAssetMenu(
        fileName = nameof(VideoSettingsConfiguration) + "_Default",
        menuName = "Configurations/SettingsModule/" + nameof(VideoSettingsConfiguration))]
    public sealed class VideoSettingsConfiguration : ScriptableObject {
        [SerializeField] private List<WindowModeOption> _windowModes = new();
        [SerializeField] private List<AntiAliasingOption> _antiAliasing = new();
        [SerializeField] private List<ShadowQualityLevel> _shadowLevels = new();
        [Tooltip("The FPS limit option that means no limit; the other options' ids are the frame rate itself.")]
        [SerializeField] private string _unlimitedFrameRateOptionId = "unlimited";

        public string UnlimitedFrameRateOptionId => _unlimitedFrameRateOptionId;

        public bool TryGetWindowMode(string optionId, out FullScreenMode mode) {
            foreach (WindowModeOption option in _windowModes)
                if (option.OptionId == optionId) {
                    mode = option.Mode;
                    return true;
                }

            mode = default;
            return false;
        }

        public bool TryGetAntiAliasingSamples(string optionId, out int samples) {
            foreach (AntiAliasingOption option in _antiAliasing)
                if (option.OptionId == optionId) {
                    samples = option.Samples;
                    return true;
                }

            samples = 1;
            return false;
        }

        public bool TryGetShadowLevel(string optionId, out ShadowQualityLevel level) {
            foreach (ShadowQualityLevel candidate in _shadowLevels)
                if (candidate.OptionId == optionId) {
                    level = candidate;
                    return true;
                }

            level = null;
            return false;
        }
    }
}
