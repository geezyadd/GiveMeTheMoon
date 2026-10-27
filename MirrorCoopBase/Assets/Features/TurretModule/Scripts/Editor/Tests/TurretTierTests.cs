using System.Collections.Generic;
using System.Linq;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Features.TurretModule.Scripts;
using Features.WeaponModule.Scripts.Definitions;
using NUnit.Framework;
using UnityEditor;

namespace Features.TurretModule.Editor.Tests {
    // The turret guns the shop sells are tiers the crew buys one after another: ordered by price, each dearer gun deals
    // more damage per second, hits at least as hard and reaches further, with its own weapon from the catalog.
    public sealed class TurretTierTests {
        private const int MIN_TIERS = 3;
        private const string WEAPON_FIELD = "_weapon";

        private List<TurretTier> _tiers;

        [SetUp]
        public void SetUp() {
            ShopCatalog shopCatalog = AddressableConfigurationLoader.Load<ShopCatalog>(Address.Configurations.ShopCatalog_Default);
            _tiers = shopCatalog.Modules
                .Where(module => module.SlotViewPrefab.GetComponent<ShipTurretStation>() != null)
                .Select(module => new TurretTier(module, ReadWeapon(module)))
                .OrderBy(tier => tier.Module.Price)
                .ToList();
        }

        [Test]
        public void WhenShopIsRead_ThenItSellsAtLeastThreeTurretTiers() =>
            Assert.GreaterOrEqual(_tiers.Count, MIN_TIERS, $"The shop sells {_tiers.Count} turret guns: {Describe()}");

        [Test]
        public void WhenTiersAreOrderedByPrice_ThenNoTwoCostTheSame() {
            for (int i = 1; i < _tiers.Count; i++)
                Assert.Greater(_tiers[i].Module.Price, _tiers[i - 1].Module.Price, $"Two turret tiers cost the same: {Describe()}");
        }

        [Test]
        public void WhenTiersAreOrderedByPrice_ThenEachDealsMoreDamagePerSecond() {
            for (int i = 1; i < _tiers.Count; i++)
                Assert.Greater(_tiers[i].DamagePerSecond, _tiers[i - 1].DamagePerSecond, $"{_tiers[i].Module.name} is dearer but no stronger: {Describe()}");
        }

        [Test]
        public void WhenTiersAreOrderedByPrice_ThenEachHitsAtLeastAsHard() {
            for (int i = 1; i < _tiers.Count; i++)
                Assert.GreaterOrEqual(_tiers[i].Weapon.DamagePerHit, _tiers[i - 1].Weapon.DamagePerHit, $"{_tiers[i].Module.name} hits softer than the tier below: {Describe()}");
        }

        [Test]
        public void WhenTiersAreOrderedByPrice_ThenEachReachesFurther() {
            for (int i = 1; i < _tiers.Count; i++)
                Assert.Greater(_tiers[i].Weapon.RangeMeters, _tiers[i - 1].Weapon.RangeMeters, $"{_tiers[i].Module.name} does not reach further: {Describe()}");
        }

        [Test]
        public void WhenTiersAreRead_ThenEachFiresItsOwnCatalogWeapon() {
            WeaponCatalog weaponCatalog = AddressableConfigurationLoader.Load<WeaponCatalog>(Address.Configurations.WeaponCatalog_Default);

            foreach (TurretTier tier in _tiers)
                Assert.IsTrue(weaponCatalog.Weapons.Contains(tier.Weapon), $"{tier.Module.name} fires {tier.Weapon.name}, which is not in {weaponCatalog.name}.");

            Assert.AreEqual(_tiers.Count, _tiers.Select(tier => tier.Weapon).Distinct().Count(), $"Two turret tiers fire the same weapon: {Describe()}");
        }

        private static WeaponDefinition ReadWeapon(ModuleDefinition module) {
            ShipTurretStation station = module.SlotViewPrefab.GetComponent<ShipTurretStation>();
            WeaponDefinition weapon = new SerializedObject(station).FindProperty(WEAPON_FIELD).objectReferenceValue as WeaponDefinition;
            Assert.IsNotNull(weapon, $"The turret of {module.name} has no weapon.");
            return weapon;
        }

        private string Describe() =>
            string.Join("; ", _tiers.Select(tier => $"{tier.Module.Id} {tier.Module.Price}: {tier.Weapon.ShotsPerSecond}/s x {tier.Weapon.DamagePerHit} = {tier.DamagePerSecond} dps, {tier.Weapon.RangeMeters} m"));
    }
}
