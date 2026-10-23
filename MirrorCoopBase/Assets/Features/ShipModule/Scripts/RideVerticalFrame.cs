using Features.CharacterMovableModule.Scripts;
using Features.FloatingControllerModule;

namespace Features.ShipModule.Scripts {
    // Everything one RideVertical frame reads: the rider's own float and jump tuning and the deck it stands on.
    public readonly struct RideVerticalFrame {
        public readonly HoverSpringSettings Spring;
        public readonly JumpTuning Jump;
        // Ship-space height of the rider pivot standing at rest on the deck.
        public readonly float RestHeight;
        // Ship-space height of the rider pivot with its collider on the deck.
        public readonly float FloorHeight;
        public readonly float Gravity;
        public readonly float Step;

        public RideVerticalFrame(
            HoverSpringSettings spring,
            JumpTuning jump,
            float restHeight,
            float floorHeight,
            float gravity,
            float step) {
            Spring = spring;
            Jump = jump;
            RestHeight = restHeight;
            FloorHeight = floorHeight;
            Gravity = gravity;
            Step = step;
        }
    }
}
