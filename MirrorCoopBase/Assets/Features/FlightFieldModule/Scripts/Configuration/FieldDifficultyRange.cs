using System;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // One number of a difficulty profile: its value at difficulty 0 and at difficulty 1, straight in between.
    [Serializable]
    public sealed class FieldDifficultyRange {
        [SerializeField] private float _easy;
        [SerializeField] private float _hard;

        public float Easy => _easy;
        public float Hard => _hard;

        public FieldDifficultyRange(float easy, float hard) {
            _easy = easy;
            _hard = hard;
        }

        public float Evaluate(float difficulty) =>
            Mathf.Lerp(_easy, _hard, difficulty);
    }
}
