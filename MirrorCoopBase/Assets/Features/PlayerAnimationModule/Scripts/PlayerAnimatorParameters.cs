using UnityEngine;

namespace Features.PlayerAnimationModule {
    // The player animator's parameters: the names the controller is built with and the hashes the driver sets. A new
    // parameter is one line here, one in PlayerAnimatorBuilder and one in PlayerAnimationDriver.
    public static class PlayerAnimatorParameters {
        // Float 0..1: idle (0) to walking forward (1), from the walk input.
        public const string ForwardName = "Forward";
        // Bool: the body has been off the ground long enough (a jump, a fall, a drift).
        public const string FlyName = "Fly";
        // The layer that shows the fall over the locomotion; the driver fades its weight with Fly.
        public const string FlyLayerName = "Fly";

        // Trigger: start the slap from its first frame (again, if it is already playing).
        public const string SlapName = "Slap";
        // The layer that shows the slap over the upper body.
        public const string SlapLayerName = "Slap";
        // The slap layer's state that plays the slap.
        public const string SlapStateName = "Slap";

        public static readonly int Forward = Animator.StringToHash(ForwardName);
        public static readonly int Fly = Animator.StringToHash(FlyName);
    }
}
