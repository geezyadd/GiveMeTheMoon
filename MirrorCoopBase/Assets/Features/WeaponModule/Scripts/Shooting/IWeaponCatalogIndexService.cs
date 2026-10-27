using Features.WeaponModule.Scripts.Definitions;

namespace Features.WeaponModule.Scripts.Shooting {
    // A weapon's place in the WeaponCatalog: the index a holder reports its hits with.
    public interface IWeaponCatalogIndexService {
        public int GetIndex(WeaponDefinition weapon);
    }
}
