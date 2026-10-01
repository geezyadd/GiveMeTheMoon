using System.Collections.Generic;
using Features.CharacterMovableModule.Scripts.PlayerStats;
using Features.StatsModule.EntityStatsModule.Scripts.Modifier;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace Features.CharacterMovableModule.Scripts.Editor {
    public sealed class PlayerStatDefaultsTests {
        private const string ASSET_PATH =
            "Assets/Features/CharacterMovableModule/GameResources/Configurations/PlayerStatsConfiguration_Default.asset";

        private const float SPRINT_SPEED = 8f;
        private const float SPRINT_BONUS = 0.5f;
        private const float SPRINT_WITH_BONUS = 12f;

        [Test]
        public void WhenDefaultAssetLoaded_ThenPrefabLocomotionValuesArePresent() {
            PlayerStatsConfiguration configuration = AssetDatabase.LoadAssetAtPath<PlayerStatsConfiguration>(ASSET_PATH);
            Assert.NotNull(configuration);
            Assert.IsTrue(configuration.CameraRelative);
            Assert.AreEqual(5f, DefaultOf(configuration, PlayerStatType.WalkSpeed), 0.0001f);
            Assert.AreEqual(SPRINT_SPEED, DefaultOf(configuration, PlayerStatType.SprintSpeed), 0.0001f);
            Assert.AreEqual(40f, DefaultOf(configuration, PlayerStatType.Acceleration), 0.0001f);
            Assert.AreEqual(80f, DefaultOf(configuration, PlayerStatType.Deceleration), 0.0001f);
            Assert.AreEqual(0.12f, DefaultOf(configuration, PlayerStatType.HaltWindow), 0.0001f);
            Assert.AreEqual(0.075f, DefaultOf(configuration, PlayerStatType.AirControl), 0.0001f);
            Assert.AreEqual(250f, DefaultOf(configuration, PlayerStatType.MaxDrive), 0.0001f);
            Assert.AreEqual(800f, DefaultOf(configuration, PlayerStatType.MaxBrake), 0.0001f);
            Assert.AreEqual(4f, DefaultOf(configuration, PlayerStatType.JumpHeight), 0.0001f);
            Assert.AreEqual(0.1f, DefaultOf(configuration, PlayerStatType.JumpCooldown), 0.0001f);
            Assert.AreEqual(0.2f, DefaultOf(configuration, PlayerStatType.CoyoteTime), 0.0001f);
            Assert.AreEqual(0.2f, DefaultOf(configuration, PlayerStatType.JumpBuffer), 0.0001f);
            Assert.AreEqual(1.2f, DefaultOf(configuration, PlayerStatType.RiseGravity), 0.0001f);
            Assert.AreEqual(3f, DefaultOf(configuration, PlayerStatType.FallGravity), 0.0001f);
            Assert.AreEqual(2f, DefaultOf(configuration, PlayerStatType.ShortHopGravity), 0.0001f);
            Assert.AreEqual(0f, DefaultOf(configuration, PlayerStatType.ExtraAirJumps), 0.0001f);
            Assert.AreEqual(12f, DefaultOf(configuration, PlayerStatType.LookTurnRate), 0.0001f);
        }

        [Test]
        public void WhenSprintModifierAdded_ThenFullSpeedIncludesPercent() {
            PlayerStatsConfiguration configuration = AssetDatabase.LoadAssetAtPath<PlayerStatsConfiguration>(ASSET_PATH);
            DiContainer container = new DiContainer();
            container.Bind<PlayerStatsConfiguration>().FromInstance(configuration).AsSingle();
            container.Bind<IStatFactory<PlayerStatType>>().To<PlayerStatFactory>().AsSingle();
            container.Bind<IStatEntityFactory<PlayerStatType>>().To<PlayerStatEntityFactory>().AsSingle();
            GameObject host = new GameObject("PlayerStatDefaultsTests");
            try {
                PlayerStatEntity entity = host.AddComponent<PlayerStatEntity>();
                container.InjectGameObject(host);
                Assert.AreEqual(SPRINT_SPEED, entity.Read(PlayerStatType.SprintSpeed), 0.0001f);
                entity.AddModifier(PlayerStatType.SprintSpeed, new StatModifier(SPRINT_BONUS, ModifierType.PercentAdd));
                Assert.AreEqual(SPRINT_WITH_BONUS, entity.Read(PlayerStatType.SprintSpeed), 0.0001f);
                Assert.AreEqual(5f, entity.Read(PlayerStatType.WalkSpeed), 0.0001f);
            }
            finally {
                Object.DestroyImmediate(host);
            }
        }

        private static float DefaultOf(PlayerStatsConfiguration configuration, PlayerStatType type) {
            IReadOnlyList<PlayerStatsConfiguration.PlayerStatDefault> defaults = configuration.Defaults;
            for (int i = 0; i < defaults.Count; i++) {
                if (defaults[i].Type == type)
                    return defaults[i].Value;
            }

            Assert.Fail($"Missing default for {type}");
            return 0f;
        }
    }
}
