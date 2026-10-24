using Features.NetworkModelModule.Scripts;
using UnityEngine;

namespace Features.FlightFieldModule.Contracts {
    // One target balloon popped on the server: what it was, where, why and by whom (an empty key when nobody shot it),
    // with which weapon (an empty id when it was not a weapon), and in which field of which loop.
    public readonly struct FieldTargetPopped {
        public FieldTargetDescription Target { get; }
        public FieldTargetPopCause Cause { get; }
        public PlayerKey Shooter { get; }
        public string WeaponId { get; }
        public int FieldNumber { get; }
        public int LoopIndex { get; }
        public FieldTargetId Id => Target.Id;
        public string KindId => Target.KindId;
        public int Reward => Target.Reward;
        public Vector3 Position => Target.Center;

        public FieldTargetPopped(FieldTargetDescription target, FieldTargetPopCause cause, PlayerKey shooter, string weaponId, int fieldNumber, int loopIndex) {
            Target = target;
            Cause = cause;
            Shooter = shooter;
            WeaponId = weaponId;
            FieldNumber = fieldNumber;
            LoopIndex = loopIndex;
        }
    }
}
