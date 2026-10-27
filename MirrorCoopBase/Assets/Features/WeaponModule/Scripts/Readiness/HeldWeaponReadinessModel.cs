using System;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Readiness {
    // The local player's weapon in hand as the HUD shows it: whether one is held and how ready its next shot is
    // (0 = just fired, 1 = ready).
    public sealed class HeldWeaponReadinessModel {
        public bool IsHolding { get; private set; }
        public float Readiness { get; private set; }

        public event Action OnReadinessChanged;

        public void SetReadiness(float readiness) {
            if (IsHolding && Mathf.Approximately(Readiness, readiness))
                return;

            IsHolding = true;
            Readiness = readiness;
            OnReadinessChanged?.Invoke();
        }

        public void Clear() {
            if (IsHolding == false)
                return;

            IsHolding = false;
            Readiness = 0f;
            OnReadinessChanged?.Invoke();
        }
    }
}
