using FMODUnity;
using UnityEngine;

namespace Features.AudioModule.Ambience {
    // The long background sounds of the game's places.
    [CreateAssetMenu(fileName = "AmbienceConfiguration", menuName = "Audio/Ambience Configuration")]
    public sealed class AmbienceConfiguration : ScriptableObject {
        [Tooltip("The FMOD event that plays from entering the session (the lobby) until leaving to the menu. Empty: silence.")]
        [SerializeField] private EventReference _sessionMusic;

        public EventReference SessionMusic => _sessionMusic;
    }
}
