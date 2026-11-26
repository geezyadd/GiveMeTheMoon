using Features.WeaponModule.Scripts.Network;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Core {
    public sealed class WeaponService : IWeaponService {
        private readonly WeaponModel _model;

        public WeaponService(WeaponModel model) =>
            _model = model;

        public bool HasWeapon => Current != null && _model.Holder != null;

        // A weapon that is a Unity object may be destroyed with its scene while still in the model.
        public IWeapon Current =>
            _model.Current is Object unityObject && unityObject == null ? null : _model.Current;

        public WeaponHolderSync Holder => _model.Holder;

        public void Equip(IWeapon weapon, WeaponHolderSync holder) =>
            _model.SetCurrent(weapon, holder);

        public void Unequip(WeaponHolderSync holder) {
            if (ReferenceEquals(_model.Holder, holder))
                _model.SetCurrent(null, null);
        }

        public bool TryGetCapability<TCapability>(out TCapability capability) where TCapability : class {
            IWeapon current = Current;
            if (current != null)
                return current.TryGetCapability(out capability);

            capability = null;
            return false;
        }
    }
}
