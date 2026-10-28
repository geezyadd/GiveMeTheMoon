using System.Linq;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Feel;
using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.Effects;
using Features.WeaponModule.Scripts.Parts.FireModes;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using Zenject;

namespace Features.HandWeaponModule.Editor.Tests {
    // Guards the hand weapons the shop sells as data: every item can be held and fires a catalog weapon, the pistol
    // keeps its rules (one strong shot that breaks a small rock, then a 2 s cooldown) and the harpoon throws a rope
    // that hooks and pulls, reaches 60-80 m and does not break before its own range.
    public sealed class HandWeaponShopGuardTests {
        private const string PISTOL_ID = "pistol";
        private const string HARPOON_ID = "harpoon";
        private const float HARPOON_MIN_RANGE_METERS = 60f;
        private const float HARPOON_MAX_RANGE_METERS = 80f;
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
        public void WhenHarpoonIsRead_ThenItThrowsOneRopePerPressThatHooksAndPulls() {
            WeaponDefinition weapon = FindHandWeapon(HARPOON_ID).Weapon;

            Assert.IsInstanceOf<SingleShotFireModeDefinition>(weapon.FireMode, "The harpoon does not throw one rope per press.");
            Assert.IsInstanceOf<RopeDeliveryDefinition>(weapon.Delivery, "The harpoon does not throw a rope.");
            Assert.IsTrue(weapon.HitEffects.Any(effect => effect is HookAndPullHitEffect), "The harpoon's rope does not hook and pull.");
        }

        [Test]
        public void WhenHarpoonIsRead_ThenItReachesBetween60And80MetersAndItsRopeHoldsThere() {
            WeaponDefinition weapon = FindHandWeapon(HARPOON_ID).Weapon;
            HookAndPullHitEffect pull = weapon.HitEffects.OfType<HookAndPullHitEffect>().Single();

            Assert.That(weapon.RangeMeters, Is.InRange(HARPOON_MIN_RANGE_METERS, HARPOON_MAX_RANGE_METERS), "The harpoon's range is outside 60-80 m.");
            Assert.GreaterOrEqual(pull.MaxRopeMeters, weapon.RangeMeters, "The rope breaks before the harpoon's own range.");
            Assert.Greater(pull.PullSpeedMetersPerSecond, 0f, "The rope does not pull.");
        }

        private IWeaponFireMode CreatePistolFireMode() {
            WeaponDefinition weapon = FindPistol().Weapon;
            Assert.IsInstanceOf<SingleShotFireModeDefinition>(weapon.FireMode, "The pistol does not fire one shot per press.");
            Assert.AreEqual(1f / PISTOL_COOLDOWN_SECONDS, weapon.ShotsPerSecond, 0.0001f, "The pistol's cooldown is not 2 s.");
            return weapon.FireMode.CreateFireMode(weapon.ShotsPerSecond);
        }

        private HandWeaponDefinition FindPistol() =>
            FindHandWeapon(PISTOL_ID);

        private HandWeaponDefinition FindHandWeapon(string id) {
            HandWeaponDefinition found = _shopCatalog.HandWeapons.FirstOrDefault(handWeapon => handWeapon.Id == id);
            Assert.IsNotNull(found, $"{_shopCatalog.name} does not sell '{id}'.");
            return found;
        }

        private static void Run(IWeaponFireMode fireMode, bool isTriggerHeld, float seconds) {
            for (float elapsed = 0f; elapsed < seconds - FRAME_SECONDS * 0.5f; elapsed += FRAME_SECONDS)
                fireMode.Advance(isTriggerHeld, FRAME_SECONDS);
        }
    }
}
