using System;
using System.Collections.Generic;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Definitions {
    // Every weapon the game knows; WeaponCatalogValidator checks it at startup.
    [CreateAssetMenu(menuName = "Game/Weapon Catalog", fileName = "WeaponCatalog_Default")]
    public sealed class WeaponCatalog : ScriptableObject {
        [SerializeField] private WeaponDefinition[] _weapons = Array.Empty<WeaponDefinition>();

        public IReadOnlyList<WeaponDefinition> Weapons => _weapons;
    }
}
