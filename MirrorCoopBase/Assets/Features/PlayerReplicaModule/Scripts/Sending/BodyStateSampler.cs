using Features.PlayerReplicaModule.Packets;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.MovementControls;
using Features.FloatingControllerModule;
using UnityEngine;

namespace Features.PlayerReplicaModule.Sending {
    // Takes the owner's state packet from its body: position and velocity, the controls it simulates from, the look,
    // standing or flying, and the jump count. Everything is in world axes for now: the still world is platform 0.
    public sealed class BodyStateSampler {
        private const uint WORLD_PLATFORM = 0;

        private readonly CharacterMovableBase _movable;
        private readonly FloatingController _floatingController;

        public BodyStateSampler(CharacterMovableBase movable, FloatingController floatingController) {
            _movable = movable;
            _floatingController = floatingController;
        }

        public BodyStatePacket Sample(ushort sequence) {
            Rigidbody body = _movable.Body;
            IMovementControls input = _movable.OwnInput;
            Vector3 wish = input != null ? input.ReadWish() : Vector3.zero;
            return new BodyStatePacket {
                Number = sequence,
                CarrierId = WORLD_PLATFORM,
                RelativePosition = body.position,
                RelativeVelocity = body.linearVelocity,
                WalkDirection = new Vector2(wish.x, wish.z),
                LookYaw = YawCompression.Pack(_movable.LookYaw),
                States = ReadFlags(input),
                JumpCount = _movable.JumpCount
            };
        }

        private BodyStateFlags ReadFlags(IMovementControls input) {
            BodyStateFlags flags = BodyStateFlags.None;
            if (input != null && input.SprintHeld)
                flags |= BodyStateFlags.Sprinting;
            if (input != null && input.JumpHeld)
                flags |= BodyStateFlags.JumpHeld;
            if (_floatingController != null && _floatingController.HasSupport)
                flags |= BodyStateFlags.Grounded;

            return flags;
        }
    }
}
