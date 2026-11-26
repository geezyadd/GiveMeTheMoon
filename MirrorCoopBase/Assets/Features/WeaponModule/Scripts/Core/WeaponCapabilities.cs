using UnityEngine;

namespace Features.WeaponModule.Scripts.Core {
    // A weapon's capabilities: the weapon itself or a component on the weapon's object.
    public static class WeaponCapabilities {
        public static bool TryGet<TCapability>(object weapon, Component owner, out TCapability capability) where TCapability : class {
            if (weapon is TCapability own) {
                capability = own;
                return true;
            }

            if (owner != null && owner.TryGetComponent(out TCapability component)) {
                capability = component;
                return true;
            }

            capability = null;
            return false;
        }
    }
}
