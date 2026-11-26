using UnityEngine;

namespace Features.TargetModule {
    // An attack landing on a target, on the peer where it landed.
    public readonly struct TargetHit {
        // The attacker's object.
        public readonly GameObject Source;
        // The velocity of the limb that hit (the slapping hand), world axes.
        public readonly Vector3 LimbVelocity;
        // From the limb to the target's aim point.
        public readonly Vector3 LimbToAimPoint;
        // True for a contact of the limb with the target, false for the limb coming close.
        public readonly bool IsTouch;

        public TargetHit(GameObject source, Vector3 limbVelocity, Vector3 limbToAimPoint, bool isTouch) {
            Source = source;
            LimbVelocity = limbVelocity;
            LimbToAimPoint = limbToAimPoint;
            IsTouch = isTouch;
        }
    }
}
