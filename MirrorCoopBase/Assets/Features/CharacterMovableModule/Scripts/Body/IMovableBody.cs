using UnityEngine;

namespace Features.CharacterMovableModule.Scripts.Body {
    // The physical body the movement drives: a single capsule or a whole ragdoll. The movement decides where to walk
    // and when to jump; the body only takes the pushes and says what it stands on.
    public interface IMovableBody {
        Vector3 Velocity { get; }
        float Mass { get; }
        // A kinematic body is moved by someone else (a ride, a seat) and is not simulated.
        bool IsKinematic { get; }
        // Whether the body can sense the ground at all; without it the body neither jumps nor gets the air gravity.
        bool SensesGround { get; }
        bool HasSupport { get; }
        Vector3 SupportNormal { get; }

        // Pushes the whole body, mass-independent, for this physics step.
        void AddAcceleration(Vector3 acceleration);

        // Changes the whole body's velocity at once (a jump).
        void AddVelocityChange(Vector3 velocityChange);

        // Lets go of the ground for a moment so a jump is not held down.
        void PauseSupport();
    }
}
