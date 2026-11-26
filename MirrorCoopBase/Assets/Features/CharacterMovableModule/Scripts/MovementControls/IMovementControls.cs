using UnityEngine;

namespace Features.CharacterMovableModule.Scripts.MovementControls {
    // Where a simulated body takes its controls from: the keyboard for the player's own body, the last state packet for
    // a copy of someone else's. The body simulation does not care which.
    public interface IMovementControls {
        bool SprintHeld { get; }

        // Held decides the jump height: a short tap gets the short-hop gravity.
        bool JumpHeld { get; }

        // The walk direction in world axes, flat, at most one long, before it is laid on the ground's slope.
        Vector3 ReadWish();

        // True once for each new jump press; called once per rendered frame.
        bool ConsumeJumpPress();

        // Grows by one with every slap press; what the owner sends to the copies.
        byte SlapCount { get; }

        // True once for each new slap press (once for several missed ones); called once per rendered frame.
        bool ConsumeSlapPress();
    }
}
