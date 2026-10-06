using Features.ShipModule.Scripts.Generated;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipStatSheetTests {
        private const string STATS_CONFIGURATION_PATH =
            "Assets/Features/ShipModule/GameResources/Configurations/ShipAccumulativeStatsConfiguration_Default.asset";
        private const float TOLERANCE = 0.0001f;
        private const string LEFT_ENGINE_SOCKET = "engine-left";
        private const string RIGHT_ENGINE_SOCKET = "engine-right";

        // The values ShipModules.ApplyDefaultStats had in code before they moved to the configuration.
        private const float THRUST_MAX = 999f;
        private const float THRUST = 0f;
        private const float FLIGHT_SPEED_MAX = 99f;
        private const float FLIGHT_SPEED = 1f;
        private const float DODGE_RANGE_MAX = 20f;
        private const float DODGE_RANGE = 1f;
        private const float HANDLING_MAX = 20f;
        private const float HANDLING = 1f;
        private const float ARMOR_MAX = 100f;
        private const float ARMOR = 100f;

        private const float ENGINE_FLIGHT_SPEED = 1f;
        private const float SMALL_ENGINE_FLIGHT_SPEED = 0.5f;

        private GameObject _host;
        private ShipStatEntity _entity;
        private ShipStatSheet _sheet;

        [SetUp]
        public void SetUp() {
            ShipAccumulativeStatsConfiguration configuration =
                AssetDatabase.LoadAssetAtPath<ShipAccumulativeStatsConfiguration>(STATS_CONFIGURATION_PATH);
            DiContainer container = new DiContainer();
            container.Bind<ShipAccumulativeStatsConfiguration>().FromInstance(configuration).AsSingle();
            container.Bind<IStatFactory<ShipStatType>>().To<ShipStatFactory>().AsSingle();
            container.Bind<IStatEntityFactory<ShipStatType>>().To<ShipStatEntityFactory>().AsSingle();
            _host = new GameObject(nameof(ShipStatSheetTests));
            _entity = _host.AddComponent<ShipStatEntity>();
            container.InjectGameObject(_host);
            _sheet = new ShipStatSheet(_entity);
            _sheet.ApplyDefaults(configuration.Defaults);
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_host);

        [Test]
        public void WhenDefaultsApplied_ThenStatsMatchTheFormerCodeValues() {
            AssertStat(ShipStatType.Thrust, THRUST_MAX, THRUST);
            AssertStat(ShipStatType.FlightSpeed, FLIGHT_SPEED_MAX, FLIGHT_SPEED);
            AssertStat(ShipStatType.DodgeRange, DODGE_RANGE_MAX, DODGE_RANGE);
            AssertStat(ShipStatType.Handling, HANDLING_MAX, HANDLING);
            AssertStat(ShipStatType.Armor, ARMOR_MAX, ARMOR);
        }

        [Test]
        public void WhenEngineInstalled_ThenFinalFlightSpeedAndSnapshotIncludeIt() {
            _sheet.SetModuleFlightSpeed(LEFT_ENGINE_SOCKET, ENGINE_FLIGHT_SPEED);

            Assert.AreEqual(FLIGHT_SPEED + ENGINE_FLIGHT_SPEED, _sheet.GetFull(ShipStatType.FlightSpeed), TOLERANCE);
            ShipStatsState state = _sheet.CreateState();
            Assert.AreEqual(FLIGHT_SPEED + ENGINE_FLIGHT_SPEED, state.FlightSpeed, TOLERANCE);
            Assert.AreEqual(THRUST, state.Thrust, TOLERANCE);
            Assert.AreEqual(DODGE_RANGE, state.DodgeRange, TOLERANCE);
            Assert.AreEqual(HANDLING, state.Handling, TOLERANCE);
            Assert.AreEqual(ARMOR, state.Armor, TOLERANCE);
        }

        [Test]
        public void WhenTwoEnginesInstalled_AndOneUninstalled_ThenSnapshotKeepsOnlyTheOther() {
            _sheet.SetModuleFlightSpeed(LEFT_ENGINE_SOCKET, ENGINE_FLIGHT_SPEED);
            _sheet.SetModuleFlightSpeed(RIGHT_ENGINE_SOCKET, SMALL_ENGINE_FLIGHT_SPEED);
            Assert.AreEqual(
                FLIGHT_SPEED + ENGINE_FLIGHT_SPEED + SMALL_ENGINE_FLIGHT_SPEED,
                _sheet.CreateState().FlightSpeed,
                TOLERANCE);

            Assert.IsTrue(_sheet.RemoveModule(LEFT_ENGINE_SOCKET));

            Assert.AreEqual(FLIGHT_SPEED + SMALL_ENGINE_FLIGHT_SPEED, _sheet.CreateState().FlightSpeed, TOLERANCE);
        }

        [Test]
        public void WhenEngineReinstalledInTheSameSocket_ThenItsSpeedCountsOnce() {
            _sheet.SetModuleFlightSpeed(LEFT_ENGINE_SOCKET, ENGINE_FLIGHT_SPEED);
            _sheet.SetModuleFlightSpeed(LEFT_ENGINE_SOCKET, SMALL_ENGINE_FLIGHT_SPEED);

            Assert.AreEqual(FLIGHT_SPEED + SMALL_ENGINE_FLIGHT_SPEED, _sheet.CreateState().FlightSpeed, TOLERANCE);
        }

        [Test]
        public void WhenEmptySocketUninstalled_ThenNothingChanges() {
            Assert.IsFalse(_sheet.RemoveModule(LEFT_ENGINE_SOCKET));

            Assert.AreEqual(FLIGHT_SPEED, _sheet.CreateState().FlightSpeed, TOLERANCE);
        }

        private void AssertStat(ShipStatType type, float maxValue, float value) {
            Assert.AreEqual(maxValue, _entity.GetStat(type).MaxValue, TOLERANCE, type + " max");
            Assert.AreEqual(value, _sheet.GetFull(type), TOLERANCE, type + " value");
        }
    }
}
