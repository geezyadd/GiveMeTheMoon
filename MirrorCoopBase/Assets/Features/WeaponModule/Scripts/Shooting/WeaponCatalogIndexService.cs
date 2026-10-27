using System;
using System.Collections.Generic;
using Features.WeaponModule.Scripts.Definitions;

namespace Features.WeaponModule.Scripts.Shooting {
    // Matched by the stable id, not by reference: in a player build an item from the scene's spawn prefabs holds its
    // own copy of the definition, apart from the one the Addressables catalog loads.
    public sealed class WeaponCatalogIndexService : IWeaponCatalogIndexService {
        private const string NOT_IN_CATALOG_FORMAT = "Weapon {0} is not in the {1}; add it there, the server checks every hit against the catalog.";

        private readonly WeaponCatalog _weaponCatalog;

        public WeaponCatalogIndexService(WeaponCatalog weaponCatalog) =>
            _weaponCatalog = weaponCatalog;

        public int GetIndex(WeaponDefinition weapon) {
            for (int i = 0; i < _weaponCatalog.Weapons.Count; i++) {
                if (string.Equals(_weaponCatalog.Weapons[i].Id, weapon.Id, StringComparison.Ordinal))
                    return i;
            }

            throw new KeyNotFoundException(string.Format(NOT_IN_CATALOG_FORMAT, weapon.Id, _weaponCatalog.name));
        }
    }
}
