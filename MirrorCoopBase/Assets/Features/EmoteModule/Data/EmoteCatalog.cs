using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

namespace Features.EmoteModule.Data {
    // Every emote a player can play; the wheel has one sector per entry, in this order (the first at the top, then
    // clockwise). An entry's index is what goes over the network, so a change of the order is a change for every peer.
    [CreateAssetMenu(
        fileName = nameof(EmoteCatalog) + "_Default",
        menuName = "Configurations/EmoteModule/" + nameof(EmoteCatalog))]
    public sealed class EmoteCatalog : ScriptableObject {
        [SerializeField] private List<EmoteEntry> _emotes = new();
        [Tooltip("The FMOD event (a loop) that plays from a player while they emote, heard near them. Empty: silence.")]
        [SerializeField] private EventReference _music;

        public IReadOnlyList<EmoteEntry> Emotes => _emotes;
        public EventReference Music => _music;

        public void SetEmotes(IEnumerable<EmoteEntry> emotes) {
            _emotes.Clear();
            _emotes.AddRange(emotes);
        }
    }
}
