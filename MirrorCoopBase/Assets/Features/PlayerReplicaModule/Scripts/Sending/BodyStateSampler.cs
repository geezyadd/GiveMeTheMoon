using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.MovementControls;
using Features.FloatingControllerModule;
using Features.PlayerReplicaModule.Packets;
using UnityEngine;

namespace Features.PlayerReplicaModule.Sending {
    // Takes the owner's state packet from its body: position and velocity, the controls it simulates from, the look,
    // standing or flying, and the jump count. On a carrier (a moving platform) everything is in the carrier's axes and
    // the carrier's id goes along, so a copy can stand on the same spot of its own copy of the carrier; on the still
    // world (carrier 0) it is in world axes. The position, the velocity and the walk direction follow the carrier's
    // pose of the last physics step, which is where the body was carried to; walking and looking follow its heading only.
    public sealed class BodyStateSampler {
        private const uint WORLD_CARRIER = 0;

        private readonly CharacterMovableBase _movable;
        private readonly FloatingController _floatingController;
        private readonly ICarrierRider _rider;

        public BodyStateSampler(CharacterMovableBase movable, FloatingController floatingController, ICarrierRider rider) {
            _movable = movable;
            _floatingController = floatingController;
            _rider = rider;
        }

        public BodyStatePacket Sample(ushort number) {
            Rigidbody body = _movable.Body;
            IMovementControls input = _movable.OwnInput;
            Vector3 position = body.position;
            Vector3 velocity = body.linearVelocity;
            Vector3 walk = input != null ? input.ReadWish() : Vector3.zero;
            float look = _movable.LookYaw;
            uint carrierId = WORLD_CARRIER;

            ICarrier carrier = _rider?.Carrier;
            if (carrier != null) {
                Pose frame = carrier.PoseBefore;
                Quaternion heading = CarrierMotion.HeadingOf(frame);
                carrierId = carrier.CarrierId;
                position = CarrierMotion.ToLocalPoint(frame, position);
                velocity = CarrierMotion.ToLocalDirection(frame, velocity);
                walk = Quaternion.Inverse(heading) * walk;
                look -= heading.eulerAngles.y;
            }

            return new BodyStatePacket {
                Number = number,
                CarrierId = carrierId,
                RelativePosition = position,
                RelativeVelocity = velocity,
                WalkDirection = new Vector2(walk.x, walk.z),
                LookYaw = YawCompression.Pack(look),
                States = ReadStates(input),
                JumpCount = _movable.JumpCount
            };
        }

        private BodyStateFlags ReadStates(IMovementControls input) {
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
