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

        public static readonly int Forward = Animator.StringToHash(ForwardName);
        public static readonly int Fly = Animator.StringToHash(FlyName);
    }
}
