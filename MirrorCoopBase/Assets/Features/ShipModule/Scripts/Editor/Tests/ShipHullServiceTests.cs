using Features.GameCoreModule.Contracts;
using Features.ShipModule.Scripts.Hull;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipHullServiceTests {
        private const float TOLERANCE = 0.0001f;
        private const float HULL_MAX = 100f;
        private const float HIT = 30f;
        private const float LETHAL_HIT = 1000f;
        private const string REPAIR_ON_LANDING_FIELD = "_repairOnLanding";

        private ShipHullConfiguration _configuration;
        private ShipHullEventBus _events;
        private int _destroyedCount;
        private int _damagedCount;
        private float _lastHullAfter;

        [SetUp]
        public void SetUp() {
            _configuration = ScriptableObject.CreateInstance<ShipHullConfiguration>();
            _events = new ShipHullEventBus();
            _events.OnDamaged += OnDamaged;
            _events.OnDestroyed += OnDestroyed;
            _destroyedCount = 0;
            _damagedCount = 0;
        }

        [TearDown]
        public void TearDown() {
            _events.OnDamaged -= OnDamaged;
            _events.OnDestroyed -= OnDestroyed;
            Object.DestroyImmediate(_configuration);
        }

        [Test]
        public void WhenHit_ThenHullDropsAndDamagedReportsTheHullLeft() {
            FakeShipHullStat hull = new FakeShipHullStat(HULL_MAX, 0f);

            CreateService(hull).ServerApplyDamage(Hit(HIT));

            Assert.AreEqual(HULL_MAX - HIT, hull.Current, TOLERANCE);
            Assert.AreEqual(1, _damagedCount);
            Assert.AreEqual(HULL_MAX - HIT, _lastHullAfter, TOLERANCE);
            Assert.AreEqual(0, _destroyedCount);
        }

        [Test]
        public void WhenHitWithArmor_ThenArmorReducesTheLoss() {
            const float armor = 50f;
            FakeShipHullStat hull = new FakeShipHullStat(HULL_MAX, armor);

            CreateService(hull).ServerApplyDamage(Hit(HIT));

            float dealt = HIT * (1f - armor * _configuration.DamageReductionPerArmor);
            Assert.AreEqual(HULL_MAX - dealt, hull.Current, TOLERANCE);
        }

        [Test]
        public void WhenHitHarderThanTheHull_ThenHullStopsAtZero() {
            FakeShipHullStat hull = new FakeShipHullStat(HULL_MAX, 0f);

            CreateService(hull).ServerApplyDamage(Hit(LETHAL_HIT));

            Assert.AreEqual(0f, hull.Current, TOLERANCE);
        }

        [Test]
        public void WhenDestroyed_AndHitAgain_ThenDestroyedFiredOnce() {
            FakeShipHullStat hull = new FakeShipHullStat(HULL_MAX, 0f);
            ShipHullService service = CreateService(hull);

            service.ServerApplyDamage(Hit(LETHAL_HIT));
            service.ServerApplyDamage(Hit(LETHAL_HIT));
            service.ServerApplyDamage(Hit(HIT));

            Assert.AreEqual(1, _destroyedCount);
            Assert.AreEqual(1, _damagedCount);
        }

        [Test]
        public void WhenNoShipIsBound_ThenHitDoesNothing() {
            new ShipHullService(new FakeShipHullStatSource(null), new ShipHullDamageRule(_configuration), _events, _configuration)
                .ServerApplyDamage(Hit(LETHAL_HIT));

            Assert.AreEqual(0, _damagedCount);
            Assert.AreEqual(0, _destroyedCount);
        }

        [Test]
        public void WhenLanded_AndRepairOnLandingIsOn_ThenHullIsFull() {
            FakeShipHullStat hull = new FakeShipHullStat(HULL_MAX, 0f);
            ShipHullService service = CreateService(hull);
            service.ServerApplyDamage(Hit(HIT));

            service.ServerRepairAfterLanding();

            Assert.AreEqual(HULL_MAX, hull.Current, TOLERANCE);
        }

        [Test]
        public void WhenLanded_AndRepairOnLandingIsOff_ThenHullStaysDamaged() {
            SerializedObject serialized = new SerializedObject(_configuration);
            serialized.FindProperty(REPAIR_ON_LANDING_FIELD).boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            FakeShipHullStat hull = new FakeShipHullStat(HULL_MAX, 0f);
            ShipHullService service = CreateService(hull);
            service.ServerApplyDamage(Hit(HIT));

            service.ServerRepairAfterLanding();

            Assert.AreEqual(HULL_MAX - HIT, hull.Current, TOLERANCE);
            Assert.AreEqual(0, hull.RepairCount);
        }

        [Test]
        public void WhenDamagedHullHealed_ThenItGetsTheHpBackUpToTheMax() {
            FakeShipHullStat hull = new FakeShipHullStat(HULL_MAX, 0f);
            ShipHullService service = CreateService(hull);
            service.ServerApplyDamage(Hit(HIT));

            service.ServerHeal(HIT / 3f);
            Assert.AreEqual(HULL_MAX - HIT + HIT / 3f, hull.Current, TOLERANCE, "A heal did not give the HP back.");

            service.ServerHeal(LETHAL_HIT);
            Assert.AreEqual(HULL_MAX, hull.Current, TOLERANCE, "A heal went past the hull max.");
        }

        [Test]
        public void WhenDestroyedHullHealed_ThenItStaysDestroyed() {
            FakeShipHullStat hull = new FakeShipHullStat(HULL_MAX, 0f);
            ShipHullService service = CreateService(hull);
            service.ServerApplyDamage(Hit(LETHAL_HIT));

            service.ServerHeal(HIT);
            Assert.AreEqual(0f, hull.Current, TOLERANCE, "A heal brought a destroyed hull back.");
        }

        private ShipHullService CreateService(IShipHullStat hull) =>
            new ShipHullService(new FakeShipHullStatSource(hull), new ShipHullDamageRule(_configuration), _events, _configuration);

        private static ShipDamage Hit(float amount) =>
            new ShipDamage(amount, ShipDamageCause.Debug, Vector3.zero);

        private void OnDamaged(ShipDamage damage, float hullAfter) {
            _damagedCount++;
            _lastHullAfter = hullAfter;
        }

        private void OnDestroyed() =>
            _destroyedCount++;
    }
}
