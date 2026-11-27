using System;
using UnityEngine;

namespace Features.EmoteModule.Data {
    // One emote of the catalog: what the wheel shows and the animator trigger that plays it.
    [Serializable]
    public sealed class EmoteEntry {
        [Tooltip("The name the wheel shows (also when there is no icon).")]
        [SerializeField] private string _displayName;
        [Tooltip("The icon the wheel shows; empty: the name only.")]
        [SerializeField] private Sprite _icon;
        [Tooltip("The animator trigger that plays the emote; the animator builder makes the trigger and a state of the same name in the Emote layer.")]
        [SerializeField] private string _triggerName;
        [Tooltip("The clip the animator builder (Tools > Animation > Build Player Animator) puts on the emote's state.")]
        [SerializeField] private AnimationClip _clip;

        public EmoteEntry(string displayName, string triggerName, AnimationClip clip) {
            _displayName = displayName;
            _triggerName = triggerName;
            _clip = clip;
        }

        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string TriggerName => _triggerName;
        public AnimationClip Clip => _clip;
    }
}
