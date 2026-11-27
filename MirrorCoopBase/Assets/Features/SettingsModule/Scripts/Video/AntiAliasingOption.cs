using System;
using UnityEngine;

namespace Features.SettingsModule.Video {
    // An anti-aliasing option of the video settings and its MSAA sample count.
    [Serializable]
    public sealed class AntiAliasingOption {
        [SerializeField] private string _optionId;
        [Tooltip("MSAA samples: 1 (off), 2, 4 or 8.")]
        [SerializeField] private int _samples = 1;

        public string OptionId => _optionId;
        public int Samples => _samples;
    }
}
