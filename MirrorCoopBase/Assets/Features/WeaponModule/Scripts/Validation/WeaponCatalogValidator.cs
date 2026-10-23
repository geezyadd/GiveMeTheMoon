using System;
using System.Collections.Generic;
using Features.WeaponModule.Scripts.Definitions;
using Zenject;

namespace Features.WeaponModule.Scripts.Validation {
    // The one startup check of the weapon catalog: every entry set, every id present and unique, the numbers usable
    // and no part missing.
    public sealed class WeaponCatalogValidator : IInitializable {
        private readonly WeaponCatalog _weaponCatalog;

        public WeaponCatalogValidator(WeaponCatalog weaponCatalog) =>
            _weaponCatalog = weaponCatalog;

        public void Initialize() {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _weaponCatalog.Weapons.Count; i++)
                ValidateWeapon(_weaponCatalog.Weapons[i], i, ids);
        }

        private void ValidateWeapon(WeaponDefinition weapon, int index, HashSet<string> ids) {
            if (weapon == null)
                throw new InvalidOperationException($"{_weaponCatalog.name}: weapon entry {index} is empty.");

            if (string.IsNullOrEmpty(weapon.Id))
                throw new InvalidOperationException($"{_weaponCatalog.name}: weapon {weapon.name} has no id.");

            if (ids.Add(weapon.Id) == false)
                throw new InvalidOperationException($"{_weaponCatalog.name}: weapon id '{weapon.Id}' is used twice (second: {weapon.name}).");

            ValidateNumbers(weapon);
            ValidateParts(weapon);
        }

        private void ValidateNumbers(WeaponDefinition weapon) {
            if (weapon.ShotsPerSecond <= 0f)
                throw new InvalidOperationException($"{_weaponCatalog.name}: weapon {weapon.Id} has no fire rate.");

            if (weapon.RangeMeters <= 0f)
                throw new InvalidOperationException($"{_weaponCatalog.name}: weapon {weapon.Id} has no range.");
        }

        private void ValidateParts(WeaponDefinition weapon) {
            if (weapon.FireMode == null)
                throw new InvalidOperationException($"{_weaponCatalog.name}: weapon {weapon.Id} has no fire mode.");

            if (weapon.Delivery == null)
                throw new InvalidOperationException($"{_weaponCatalog.name}: weapon {weapon.Id} has no shot delivery.");

            if (weapon.HitEffects.Count == 0)
                throw new InvalidOperationException($"{_weaponCatalog.name}: weapon {weapon.Id} has no hit effect.");

            for (int i = 0; i < weapon.HitEffects.Count; i++)
                if (weapon.HitEffects[i] == null)
                    throw new InvalidOperationException($"{_weaponCatalog.name}: weapon {weapon.Id} hit effect {i} is empty.");

            if (weapon.FeelProfile == null)
                throw new InvalidOperationException($"{_weaponCatalog.name}: weapon {weapon.Id} has no feel profile.");
        }
    }
}
