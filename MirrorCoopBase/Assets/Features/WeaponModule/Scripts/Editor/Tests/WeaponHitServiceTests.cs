using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Hits;
using Features.WeaponModule.Scripts.Network;
using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.Effects;
using Features.WeaponModule.Scripts.Targets;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    // The server end of the hit path: a reported hit reaches its target only through the hit rule.
    public sealed class WeaponHitServiceTests {
        private const string MINIGUN_ID = "minigun";
        private const string HARPOON_ID = "harpoon";
        private const int MINIGUN_INDEX = 0;
        private const int HARPOON_INDEX = 1;
        private const int NOT_IN_CATALOG_INDEX = 5;
        private const ulong TARGET_ID = 42UL;
        private const ulong UNKNOWN_TARGET_ID = 7UL;
        private const float DAMAGE = 3f;
        private const float HIT_DISTANCE_METERS = 10f;
        private const double NOW_SECONDS = 10d;

        private static readonly PlayerKey _shooter = new PlayerKey("client:shooter");

        private WeaponHitRuleConfiguration _configuration;
        private WeaponDefinition _minigun;
        private WeaponDefinition _harpoon;
        private WeaponCatalog _catalog;
        private WeaponTargetRegistry _targets;
        private CountingWeaponTarget _target;
        private WeaponHitService _service;
        private ServerWeaponHolding _holding;

        [SetUp]
        public void SetUp() {
            _configuration = CreateConfiguration();
            _minigun = CreateWeapon(MINIGUN_ID);
            _harpoon = CreateWeapon(HARPOON_ID);
            _catalog = CreateCatalog(_minigun, _harpoon);
            _targets = new WeaponTargetRegistry();
            _target = new CountingWeaponTarget();
            _targets.Register(TARGET_ID, _target);
            _service = new WeaponHitService(new WeaponHitRule(_configuration), _targets, _catalog);
            _holding = new ServerWeaponHolding(_minigun, MINIGUN_INDEX, _shooter, new FixedWeaponHolder());
        }

        [TearDown]
        public void TearDown() {
            Object.DestroyImmediate(_configuration);
            Object.DestroyImmediate(_minigun);
            Object.DestroyImmediate(_harpoon);
            Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void WhenHolderReportsHitOnKnownTarget_ThenTargetTakesTheHeldWeaponsHit() {
            WeaponHitVerdict verdict = Apply(MINIGUN_INDEX, TARGET_ID);

            Assert.AreEqual(WeaponHitVerdict.Accepted, verdict);
            Assert.AreEqual(1, _target.Hits);
            Assert.AreEqual(DAMAGE, _target.LastHit.Damage);
            Assert.AreEqual(_shooter, _target.LastHit.Shooter);
            Assert.AreEqual(MINIGUN_ID, _target.LastHit.WeaponId);
        }

        [Test]
        public void WhenReportNamesAnotherWeaponThanTheHeldOne_ThenTargetIsUntouched() {
            Assert.AreEqual(WeaponHitVerdict.WeaponNotHeld, Apply(HARPOON_INDEX, TARGET_ID));
            Assert.AreEqual(0, _target.Hits);
        }

        [Test]
        public void WhenReportNamesNoCatalogWeapon_ThenItIsDropped() =>
            Assert.AreEqual(WeaponHitVerdict.WeaponNotHeld, Apply(NOT_IN_CATALOG_INDEX, TARGET_ID));

        [Test]
        public void WhenTargetIsUnknown_ThenItIsDropped() =>
            Assert.AreEqual(WeaponHitVerdict.UnknownTarget, Apply(MINIGUN_INDEX, UNKNOWN_TARGET_ID));

        [Test]
        public void WhenTargetUnregisters_AndHitArrives_ThenItIsDropped() {
            _targets.Unregister(TARGET_ID);

            Assert.AreEqual(WeaponHitVerdict.UnknownTarget, Apply(MINIGUN_INDEX, TARGET_ID));
            Assert.AreEqual(0, _target.Hits);
        }

        [Test]
        public void WhenTargetIsDead_ThenTargetIsUntouched() {
            _target.IsAlive = false;

            Assert.AreEqual(WeaponHitVerdict.TargetNotAlive, Apply(MINIGUN_INDEX, TARGET_ID));
            Assert.AreEqual(0, _target.Hits);
        }

        private WeaponHitVerdict Apply(int weaponIndex, ulong targetId) =>
            _service.ServerApplyReportedHit(_holding, weaponIndex, new WeaponHitReport(targetId, Vector3.forward * HIT_DISTANCE_METERS, Vector3.back), NOW_SECONDS);

        private static WeaponHitRuleConfiguration CreateConfiguration() {
            WeaponHitRuleConfiguration configuration = ScriptableObject.CreateInstance<WeaponHitRuleConfiguration>();
            SerializedObject serialized = new SerializedObject(configuration);
            serialized.FindProperty("_burstSeconds").floatValue = 1f;
            serialized.FindProperty("_rangeToleranceMeters").floatValue = 25f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return configuration;
        }

        private static WeaponCatalog CreateCatalog(params WeaponDefinition[] weapons) {
            WeaponCatalog catalog = ScriptableObject.CreateInstance<WeaponCatalog>();
            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty("_weapons");
            list.arraySize = weapons.Length;
            for (int i = 0; i < weapons.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = weapons[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private static WeaponDefinition CreateWeapon(string id) {
            WeaponDefinition weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            SerializedObject serialized = new SerializedObject(weapon);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_shotsPerSecond").floatValue = 20f;
            serialized.FindProperty("_damagePerHit").floatValue = DAMAGE;
            serialized.FindProperty("_rangeMeters").floatValue = 400f;
            serialized.FindProperty("_delivery").managedReferenceValue = new HitscanDeliveryDefinition();
            SerializedProperty effects = serialized.FindProperty("_hitEffects");
            effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).managedReferenceValue = new DamageHitEffect();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return weapon;
        }
    }
}
