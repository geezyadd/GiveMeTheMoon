using UnityEngine;

namespace Features.CameraModule.Contracts {
    // One kind of camera shake at strength (or intensity) 1: how far it turns and moves the view, how fast and how
    // long. A one-shot shake fades out by itself; a continuous one follows the intensity its source sets.
    [CreateAssetMenu(
        fileName = nameof(CameraShakeProfile),
        menuName = "Configurations/CameraModule/" + nameof(CameraShakeProfile))]
    public sealed class CameraShakeProfile : ScriptableObject {
        [Tooltip("The largest turn of the view on pitch and yaw, degrees.")]
        [SerializeField, Min(0f)] private float _degrees = 1f;
        [Tooltip("Roll as a share of the turn on the other axes.")]
        [SerializeField, Range(0f, 1f)] private float _rollShare = 1f;
        [Tooltip("The largest move of the view sideways and up, metres.")]
        [SerializeField, Min(0f)] private float _meters;
        [Tooltip("One-shot: how long it lasts. Continuous: how long the intensity takes to rise from 0 to 1 or fall back. Seconds.")]
        [SerializeField, Min(0f)] private float _seconds = 0.35f;
        [Tooltip("How fast the view shakes, swings per second.")]
        [SerializeField, Min(0f)] private float _frequency = 18f;
        [Tooltip("One-shot fade: the shake is scaled by (1 - age / length) ^ power. 1 = linear, 2 = fast at first, then soft.")]
        [SerializeField, Min(0f)] private float _fadePower = 2f;
        [Tooltip("One-shot: a weaker shake ends sooner (length = seconds x strength), so every shake of this profile fades at the same rate.")]
        [SerializeField] private bool _isLengthScaledByStrength;

        public float Degrees => _degrees;
        public float RollShare => _rollShare;
        public float Meters => _meters;
        public float Seconds => _seconds;
        public float Frequency => _frequency;
        public float FadePower => _fadePower;
        public bool IsLengthScaledByStrength => _isLengthScaledByStrength;
    }
}
