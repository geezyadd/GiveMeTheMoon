using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.FireModes {
    // One shot per trigger press, then a cooldown of one shot interval: a press before it is over is lost, and holding
    // the trigger never fires again. Readiness climbs from 0 right after a shot to 1 when the next press may fire;
    // IsFiring is true only in the step a shot is born.
    public sealed class SingleShotFireMode : IWeaponFireMode {
        private const float READY = 1f;

        private readonly float _cooldownSeconds;

        private bool _wasTriggerHeld;

        public float Readiness { get; private set; } = READY;
        public bool IsFiring { get; private set; }

        public SingleShotFireMode(float cooldownSeconds) =>
            _cooldownSeconds = cooldownSeconds;

        public int Advance(bool isTriggerHeld, float deltaTime) {
            Recover(deltaTime);
            bool isPressed = isTriggerHeld && _wasTriggerHeld == false;
            _wasTriggerHeld = isTriggerHeld;
            IsFiring = isPressed && Readiness >= READY;
            if (IsFiring == false)
                return 0;

            Readiness = 0f;
            return 1;
        }

        private void Recover(float deltaTime) =>
            Readiness = _cooldownSeconds > 0f
                ? Mathf.Min(READY, Readiness + deltaTime / _cooldownSeconds)
                : READY;
    }
}
