using UnityEngine;

namespace Features.CharacterMovableModule.Scripts.MoveInput {
    // Where a simulated body takes its controls from: the keyboard for the player's own body, the last state packet for
    // a copy of someone else's. The body simulation does not care which.
    public interface IMoveInputSource {
        bool SprintHeld { get; }

        // Held decides the jump height: a short tap gets the short-hop gravity.
        bool JumpHeld { get; }

        // The walk direction in world axes, flat, at most one long, before it is laid on the ground's slope.
        Vector3 ReadWish();

        // True once for each new jump press; called once per rendered frame.
        bool ConsumeJumpPress();
    }
}
