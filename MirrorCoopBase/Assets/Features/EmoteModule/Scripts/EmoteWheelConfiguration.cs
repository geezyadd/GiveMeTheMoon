using UnityEngine;

namespace Features.EmoteModule {
    // How the emote wheel reads the mouse.
    [CreateAssetMenu(
        fileName = nameof(EmoteWheelConfiguration) + "_Default",
        menuName = "Configurations/EmoteModule/" + nameof(EmoteWheelConfiguration))]
    public sealed class EmoteWheelConfiguration : ScriptableObject {
        [Tooltip("How far the wheel's pointer goes from the centre, in mouse movement (pixels of the look input).")]
        [SerializeField, Min(1f)] private float _pointerRadius = 120f;
        [Tooltip("A pointer closer to the centre than this picks nothing: releasing there cancels (same units as the radius).")]
        [SerializeField, Min(0f)] private float _deadZoneRadius = 35f;

        public float PointerRadius => _pointerRadius;
        public float DeadZoneRadius => _deadZoneRadius;
    }
}
