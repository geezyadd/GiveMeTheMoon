using UnityEngine;

namespace Features.PlayerAnimationModule {
    // How the player's animator parameters follow the movement and the body.
    [CreateAssetMenu(
        fileName = nameof(PlayerAnimationConfiguration) + "_Default",
        menuName = "Configurations/PlayerAnimationModule/" + nameof(PlayerAnimationConfiguration))]
    public sealed class PlayerAnimationConfiguration : ScriptableObject {
        [Tooltip("How long Forward takes to follow the walk input, seconds (the animator's damping).")]
        [SerializeField, Min(0f)] private float _forwardDampingSeconds = 0.125f;
        [Tooltip("A walk input weaker than this shows as standing still (0..1), so the idle stays still.")]
        [SerializeField, Range(0f, 1f)] private float _forwardIdleCutoff = 0.125f;
        [Tooltip("The body must be off the ground this long before Fly turns on, seconds; a step or a bump does not count.")]
        [SerializeField, Min(0f)] private float _flyDelaySeconds = 0.2f;

        public float ForwardDampingSeconds => _forwardDampingSeconds;
        public float ForwardIdleCutoff => _forwardIdleCutoff;
        public float FlyDelaySeconds => _flyDelaySeconds;
    }
}
