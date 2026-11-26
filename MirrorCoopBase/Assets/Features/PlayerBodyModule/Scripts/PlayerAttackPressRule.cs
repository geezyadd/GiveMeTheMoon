namespace Features.PlayerBodyModule {
    // Whether the player's fire button counts as an attack press, pure.
    public static class PlayerAttackPressRule {
        public readonly struct State {
            public readonly bool IsHolding;
            public readonly bool IsSeated;
            public readonly bool IsControlBlocked;
            public readonly bool IsCursorFree;
            public readonly bool IsAttacking;

            public State(bool isHolding, bool isSeated, bool isControlBlocked, bool isCursorFree, bool isAttacking) {
                IsHolding = isHolding;
                IsSeated = isSeated;
                IsControlBlocked = isControlBlocked;
                IsCursorFree = isCursorFree;
                IsAttacking = isAttacking;
            }
        }

        // An item in the hand takes the fire button (a pistol fires, nothing slaps); a seat, a control block (death, a
        // window) or a free cursor (a click on a window) take it away; a press during an attack is not kept for later.
        public static bool Allows(in State state) =>
            state.IsHolding == false
            && state.IsSeated == false
            && state.IsControlBlocked == false
            && state.IsCursorFree == false
            && state.IsAttacking == false;
    }
}
