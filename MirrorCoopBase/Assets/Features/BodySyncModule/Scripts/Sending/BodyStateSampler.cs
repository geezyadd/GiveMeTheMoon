using Features.BodySyncModule.Packets;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.MoveInput;
using Features.FloatingControllerModule;
using UnityEngine;

namespace Features.BodySyncModule.Sending {
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
            IMoveInputSource input = _movable.OwnInput;
            Vector3 wish = input != null ? input.ReadWish() : Vector3.zero;
            return new BodyStatePacket {
                Sequence = sequence,
                PlatformId = WORLD_PLATFORM,
                LocalPosition = body.position,
                LocalVelocity = body.linearVelocity,
                Wish = new Vector2(wish.x, wish.z),
                FacingYaw = YawCompression.Pack(_movable.FacingYaw),
                Flags = ReadFlags(input),
                JumpCounter = _movable.JumpCount
            };
        }

        private BodyStateFlags ReadFlags(IMoveInputSource input) {
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
