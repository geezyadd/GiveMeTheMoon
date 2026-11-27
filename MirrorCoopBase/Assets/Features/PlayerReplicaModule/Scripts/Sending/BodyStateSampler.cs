using Features.AttackModule.Contracts;
using Features.CharacterMovableModule.Scripts;
using Features.EmoteModule.Contracts;
using Features.CharacterMovableModule.Scripts.Body;
using Features.CharacterMovableModule.Scripts.MovementControls;
using Features.FloatingControllerModule;
using Features.PlayerReplicaModule.Packets;
using UnityEngine;

namespace Features.PlayerReplicaModule.Sending {
    // Takes the owner's state packet from the body its movement drives (the capsule or the ragdoll): position and
    // velocity, the controls it simulates from, the look, standing or flying, and the jump count. On a carrier (a
    // moving platform) everything is in the carrier's axes and the carrier's id goes along, so a copy can stand on the
    // same spot of its own copy of the carrier; on the still world (carrier 0) it is in world axes. The position, the
    // velocity and the walk direction follow the carrier's pose of the last physics step, which is where the body was
    // carried to; walking and looking follow its heading only. The attack count comes from the body's attack presses,
    // the emote count and emote from its emote plays, when it has them.
    public sealed class BodyStateSampler {
        private const uint WORLD_CARRIER = 0;

        private readonly CharacterMovableBase _movable;
        private readonly IAttackPresses _attackPresses;
        private readonly IEmotePlays _emotePlays;

        public BodyStateSampler(CharacterMovableBase movable, IAttackPresses attackPresses, IEmotePlays emotePlays) {
            _movable = movable;
            _attackPresses = attackPresses;
            _emotePlays = emotePlays;
        }

        public BodyStatePacket Sample(ushort number) {
            IMovableBody body = _movable.DrivenBody;
            IMovementControls input = _movable.OwnInput;
            Vector3 position = body.Position;
            Vector3 velocity = body.Velocity;
            Vector3 walk = input != null ? input.ReadWish() : Vector3.zero;
            float look = _movable.LookYaw;
            uint carrierId = WORLD_CARRIER;

            ICarrier carrier = body.Rider?.Carrier;
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
                JumpCount = _movable.JumpCount,
                AttackCount = _attackPresses != null ? _attackPresses.AttackCount : (byte)0,
                EmoteCount = _emotePlays != null ? _emotePlays.EmoteCount : (byte)0,
                EmoteIndex = _emotePlays != null ? _emotePlays.EmoteIndex : (byte)0
            };
        }

        private BodyStateFlags ReadStates(IMovementControls input) {
            BodyStateFlags flags = BodyStateFlags.None;
            if (input != null && input.SprintHeld)
                flags |= BodyStateFlags.Sprinting;
            if (input != null && input.JumpHeld)
                flags |= BodyStateFlags.JumpHeld;
            if (_movable.IsSupported)
                flags |= BodyStateFlags.Grounded;

            return flags;
        }
    }
}
