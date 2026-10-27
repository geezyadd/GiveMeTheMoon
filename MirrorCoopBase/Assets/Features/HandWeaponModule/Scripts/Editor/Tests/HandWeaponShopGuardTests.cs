using System.Linq;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Feel;
using Features.WeaponModule.Scripts.Parts.FireModes;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using Zenject;

namespace Features.HandWeaponModule.Editor.Tests {
    // Guards the hand weapons the shop sells as data: every item can be held and fires a catalog weapon, and the
    // pistol keeps its rules: one strong shot that breaks a small rock or pops any balloon, then a 2 s cooldown.
    public sealed class HandWeaponShopGuardTests {
        private const string PISTOL_ID = "pistol";
        private const float PISTOL_COOLDOWN_SECONDS = 2f;
        private const float FRAME_SECONDS = 0.05f;
        private const float EARLY_PRESS_SECONDS = 1.5f;

        private ShopCatalog _shopCatalog;

        [SetUp]
        public void SetUp() =>
            _shopCatalog = AddressableConfigurationLoader.Load<ShopCatalog>(Address.Configurations.ShopCatalog_Default);

        [Test]
        public void WhenShopCatalogIsRead_ThenEveryHandWeaponItemCanBeHeldAndPointsBackToIt() {
            Assert.IsNotEmpty(_shopCatalog.HandWeapons, $"{_shopCatalog.name} sells no hand weapon.");
            foreach (HandWeaponDefinition handWeapon in _shopCatalog.HandWeapons) {
                HandWeaponItem item = handWeapon.ItemPrefab;
                Assert.IsNotNull(item, $"{handWeapon.name} has no item prefab.");
                Assert.AreSame(handWeapon, item.Definition, $"The item {item.name} points to another hand weapon than {handWeapon.name}.");
                Assert.IsNotNull(item.GetComponent<Grabbable>(), $"The item of {handWeapon.name} has no {nameof(Grabbable)}.");
                Assert.IsNotNull(item.GetComponent<NetworkIdentity>(), $"The item of {handWeapon.name} has no {nameof(NetworkIdentity)}.");
                Assert.IsNotNull(item.GetComponent<IWeaponFeelPresenter>(), $"The item of {handWeapon.name} has no weapon feel.");
                Assert.IsNotNull(item.GetComponent<ZenAutoInjecter>(), $"The item of {handWeapon.name} is not injected, its feel would get no camera shake.");
                Assert.Greater(handWeapon.Price, 0, $"{handWeapon.name} is free.");
                Assert.AreNotEqual(0, handWeapon.CrosshairLayers.value, $"The crosshair of {handWeapon.name} rests on nothing, its shots would fly past the target.");
            }
        }

        [Test]
        public void WhenShopCatalogIsRead_ThenEveryHandWeaponFiresACatalogWeapon() {
            WeaponCatalog weaponCatalog = AddressableConfigurationLoader.Load<WeaponCatalog>(Address.Configurations.WeaponCatalog_Default);

            string[] missing = _shopCatalog.HandWeapons
                .Where(handWeapon => weaponCatalog.Weapons.Contains(handWeapon.Weapon) == false)
                .Select(handWeapon => handWeapon.name)
                .ToArray();

            Assert.IsEmpty(missing, $"Hand weapons whose weapon is not in {weaponCatalog.name}, the server would drop their hits: {string.Join(", ", missing)}");
        }

        [Test]
        public void WhenPistolIsPressedAgainWithinTwoSeconds_ThenTheShotIsBlocked() {
            IWeaponFireMode fireMode = CreatePistolFireMode();
            Assert.AreEqual(1, fireMode.Advance(true, FRAME_SECONDS), "The first press did not fire.");
            Run(fireMode, false, EARLY_PRESS_SECONDS);

            Assert.AreEqual(0, fireMode.Advance(true, FRAME_SECONDS), $"A press {EARLY_PRESS_SECONDS} s after a shot fired again.");
        }

        [Test]
        public void WhenPistolIsPressedAgainAfterTwoSeconds_ThenItFires() {
            IWeaponFireMode fireMode = CreatePistolFireMode();
            fireMode.Advance(true, FRAME_SECONDS);
            Run(fireMode, false, PISTOL_COOLDOWN_SECONDS);

            Assert.AreEqual(1, fireMode.Advance(true, FRAME_SECONDS), $"A press {PISTOL_COOLDOWN_SECONDS} s after a shot did not fire.");
        }

        [Test]
        public void WhenPistolHits_ThenOneShotBreaksAnySmallRock() {
            FieldRockDurabilityConfiguration durability = AddressableConfigurationLoader.Load<FieldRockDurabilityConfiguration>(Address.Configurations.FieldRockDurabilityConfiguration_Default);
            float smallRockHitPoints = durability.HitPointsByDiameter.Evaluate(durability.SmallMaxDiameterMeters);

            Assert.GreaterOrEqual(FindPistol().Weapon.DamagePerHit, smallRockHitPoints, "One pistol shot does not break the largest small rock.");
        }

        [Test]
        public void WhenPistolHits_ThenOneShotPopsAnyBalloon() {
            FieldTargetCatalog targets = AddressableConfigurationLoader.Load<FieldTargetCatalog>(Address.Configurations.FieldTargetCatalog_Default);
            float damage = FindPistol().Weapon.DamagePerHit;

            string[] tooStrong = targets.Targets.Where(target => target.HitPoints > damage).Select(target => target.Id).ToArray();

            Assert.IsEmpty(tooStrong, $"One pistol shot does not pop: {string.Join(", ", tooStrong)}");
        }

        private IWeaponFireMode CreatePistolFireMode() {
            WeaponDefinition weapon = FindPistol().Weapon;
            Assert.IsInstanceOf<SingleShotFireModeDefinition>(weapon.FireMode, "The pistol does not fire one shot per press.");
            Assert.AreEqual(1f / PISTOL_COOLDOWN_SECONDS, weapon.ShotsPerSecond, 0.0001f, "The pistol's cooldown is not 2 s.");
            return weapon.FireMode.CreateFireMode(weapon.ShotsPerSecond);
        }

        private HandWeaponDefinition FindPistol() {
            HandWeaponDefinition pistol = _shopCatalog.HandWeapons.FirstOrDefault(handWeapon => handWeapon.Id == PISTOL_ID);
            Assert.IsNotNull(pistol, $"{_shopCatalog.name} does not sell the pistol.");
            return pistol;
        }

        private static void Run(IWeaponFireMode fireMode, bool isTriggerHeld, float seconds) {
            for (float elapsed = 0f; elapsed < seconds - FRAME_SECONDS * 0.5f; elapsed += FRAME_SECONDS)
                fireMode.Advance(isTriggerHeld, FRAME_SECONDS);
        }
    }
}
