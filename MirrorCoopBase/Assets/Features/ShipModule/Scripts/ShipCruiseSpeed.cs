using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The ship's own cruise speed easing to the throttle's target with an acceleration limit, so it speeds up and
    // slows down smoothly and the crew on the deck keeps its footing.
    public sealed class ShipCruiseSpeed {
        public float Current { get; private set; }

        public void Reset() =>
            Current = 0f;

        // A limit of 0 or less reaches the target at once.
        public float Step(float target, float maxAcceleration, float deltaTime) {
            float clampedTarget = Mathf.Max(0f, target);
            Current = maxAcceleration <= 0f
                ? clampedTarget
                : Mathf.MoveTowards(Current, clampedTarget, maxAcceleration * deltaTime);
            return Current;
        }
    }
}
