using System;
using UnityEngine;

namespace Features.CharacterMovableModule.Scripts.Drift {
    // How a player drifts in open space: the exit velocity fades to a slow drift, a very weak push of the move keys,
    // and the slow tumble with the dangling arms and legs every peer shows on its own.
    [Serializable]
    public sealed class CharacterDriftSettings {
        [Tooltip("How fast the speed above the slow drift fades, per second (exponential).")]
        [SerializeField, Min(0f)] private float _dampingPerSecond = 1.2f;
        [Tooltip("The slow drift the exit velocity fades to, meters per second; a slower drift keeps its speed.")]
        [SerializeField, Min(0f)] private float _driftSpeedMetersPerSecond = 0.6f;
        [Tooltip("The move keys' push, meters per second squared: very weak, a way to nudge, not to fly.")]
        [SerializeField, Min(0f)] private float _pushAcceleration = 0.35f;
        [Tooltip("The move keys never push faster than this, meters per second (a shot's recoil may).")]
        [SerializeField, Min(0f)] private float _maxPushSpeedMetersPerSecond = 1.5f;
        [Tooltip("The body's slow tumble, degrees per second.")]
        [SerializeField, Min(0f)] private float _tumbleDegreesPerSecond = 22f;
        [Tooltip("How far the arms and legs swing from their rest pose, degrees.")]
        [SerializeField, Min(0f)] private float _limbSwingDegrees = 35f;
        [Tooltip("How often the arms and legs swing, cycles per second.")]
        [SerializeField, Min(0f)] private float _limbSwingCyclesPerSecond = 0.45f;

        public float DampingPerSecond => _dampingPerSecond;
        public float DriftSpeedMetersPerSecond => _driftSpeedMetersPerSecond;
        public float PushAcceleration => _pushAcceleration;
        public float MaxPushSpeedMetersPerSecond => _maxPushSpeedMetersPerSecond;
        public float TumbleDegreesPerSecond => _tumbleDegreesPerSecond;
        public float LimbSwingDegrees => _limbSwingDegrees;
        public float LimbSwingCyclesPerSecond => _limbSwingCyclesPerSecond;

        public CharacterDriftSettings() {
        }

        public CharacterDriftSettings(float dampingPerSecond, float driftSpeedMetersPerSecond, float pushAcceleration, float maxPushSpeedMetersPerSecond) {
            _dampingPerSecond = dampingPerSecond;
            _driftSpeedMetersPerSecond = driftSpeedMetersPerSecond;
            _pushAcceleration = pushAcceleration;
            _maxPushSpeedMetersPerSecond = maxPushSpeedMetersPerSecond;
        }
    }
}
