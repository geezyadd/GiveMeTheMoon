using FMODUnity;
using UnityEngine;

namespace Features.AudioModule.Ambience {
    // The long background sounds of the game's places.
    [CreateAssetMenu(fileName = "AmbienceConfiguration", menuName = "Audio/Ambience Configuration")]
    public sealed class AmbienceConfiguration : ScriptableObject {
        [Tooltip("The FMOD event that plays from entering the session (the lobby) until leaving to the menu. Empty: silence.")]
        [SerializeField] private EventReference _sessionMusic;

        [Tooltip("The session music gives way when a dancing player is within this distance (m) of the camera.")]
        [SerializeField, Min(0f)] private float _duckRadius = 15f;
        [Tooltip("The session music's volume (0..1) while it gives way.")]
        [SerializeField, Range(0f, 1f)] private float _duckedVolume = 0f;
        [Tooltip("How fast (volume per second) the session music goes down and comes back.")]
        [SerializeField, Min(0.01f)] private float _duckSpeed = 2f;

        public EventReference SessionMusic => _sessionMusic;
        public float DuckRadius => _duckRadius;
        public float DuckedVolume => _duckedVolume;
        public float DuckSpeed => _duckSpeed;
    }
}
