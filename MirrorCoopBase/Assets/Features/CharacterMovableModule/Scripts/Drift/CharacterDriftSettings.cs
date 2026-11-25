using System;
using UnityEngine;

namespace Features.CharacterMovableModule.Scripts.Drift {
    // How a player drifts in open space: the exit velocity fades to a slow drift, the move keys give a weak push
    // along the view, and the body turns to fly head first.
    [Serializable]
    public sealed class CharacterDriftSettings {
        [Tooltip("How fast the speed above the slow drift fades, per second (exponential).")]
        [SerializeField, Min(0f)] private float _dampingPerSecond = 1.2f;
        [Tooltip("The slow drift the exit velocity fades to, meters per second; a slower drift keeps its speed.")]
        [SerializeField, Min(0f)] private float _driftSpeedMetersPerSecond = 0.6f;
        [Tooltip("The move keys' push, meters per second squared: a way to nudge, not to fly.")]
        [SerializeField, Min(0f)] private float _pushAcceleration = 3f;
        [Tooltip("The move keys never push faster than this, meters per second.")]
        [SerializeField, Min(0f)] private float _maxPushSpeedMetersPerSecond = 4f;
        [Tooltip("How fast the body turns to fly head first along its flight, per second (exponential; 2 is about a second and a half).")]
        [SerializeField, Min(0f)] private float _headFirstTurnPerSecond = 2f;
        [Tooltip("Slower than this, meters per second, the body keeps the turn it has instead of following the flight.")]
        [SerializeField, Min(0f)] private float _headFirstMinSpeedMetersPerSecond = 0.3f;

        public float DampingPerSecond => _dampingPerSecond;
        public float HeadFirstTurnPerSecond => _headFirstTurnPerSecond;
        public float HeadFirstMinSpeedMetersPerSecond => _headFirstMinSpeedMetersPerSecond;
        public float DriftSpeedMetersPerSecond => _driftSpeedMetersPerSecond;
        public float PushAcceleration => _pushAcceleration;
        public float MaxPushSpeedMetersPerSecond => _maxPushSpeedMetersPerSecond;

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
