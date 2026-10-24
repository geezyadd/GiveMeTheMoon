using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // One struck rock's feedback in progress: how long it still runs and where the rock's mesh rests.
    public struct FieldRockHitShake {
        public FieldRockTarget Target;
        public Vector3 RestLocalPosition;
        public float SecondsLeft;

        public FieldRockHitShake(FieldRockTarget target, Vector3 restLocalPosition, float secondsLeft) {
            Target = target;
            RestLocalPosition = restLocalPosition;
            SecondsLeft = secondsLeft;
        }
    }
}
