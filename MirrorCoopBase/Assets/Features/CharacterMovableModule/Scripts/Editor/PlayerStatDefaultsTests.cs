using System.Collections.Generic;
using System.Reflection;
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
        // Edit mode does not run Awake on AddComponent, so the test calls it the way Unity does on spawn.
        private const string AWAKE_METHOD = "Awake";
        private const float TOLERANCE = 0.0001f;

        private const float WALK_SPEED = 5f;
        private const float SPRINT_SPEED = 8f;
        private const float ACCELERATION = 40f;
        private const float DECELERATION = 80f;
        private const float HALT_WINDOW = 0.12f;
        private const float AIR_CONTROL = 0.075f;
        private const float MAX_DRIVE = 250f;
        private const float MAX_BRAKE = 800f;
        private const float JUMP_HEIGHT = 4f;
        private const float JUMP_COOLDOWN = 0.1f;
        private const float COYOTE_TIME = 0.2f;
        private const float JUMP_BUFFER = 0.2f;
        private const float RISE_GRAVITY = 1.2f;
        private const float FALL_GRAVITY = 3f;
        private const float SHORT_HOP_GRAVITY = 2f;
        private const float EXTRA_AIR_JUMPS = 0f;
        private const float LOOK_TURN_RATE = 12f;

        private const float SPRINT_SPEED_BONUS = 0.5f;
        private const float SPRINT_SPEED_WITH_BONUS = 12f;

        [Test]
        public void WhenDefaultAssetLoaded_ThenPrefabLocomotionValuesArePresent() {
            PlayerStatsConfiguration configuration = AssetDatabase.LoadAssetAtPath<PlayerStatsConfiguration>(ASSET_PATH);
            Assert.NotNull(configuration);
            Assert.IsTrue(configuration.CameraRelative);
            Assert.AreEqual(WALK_SPEED, DefaultOf(configuration, PlayerStatType.WalkSpeed), TOLERANCE);
            Assert.AreEqual(SPRINT_SPEED, DefaultOf(configuration, PlayerStatType.SprintSpeed), TOLERANCE);
            Assert.AreEqual(ACCELERATION, DefaultOf(configuration, PlayerStatType.Acceleration), TOLERANCE);
            Assert.AreEqual(DECELERATION, DefaultOf(configuration, PlayerStatType.Deceleration), TOLERANCE);
            Assert.AreEqual(HALT_WINDOW, DefaultOf(configuration, PlayerStatType.HaltWindow), TOLERANCE);
            Assert.AreEqual(AIR_CONTROL, DefaultOf(configuration, PlayerStatType.AirControl), TOLERANCE);
            Assert.AreEqual(MAX_DRIVE, DefaultOf(configuration, PlayerStatType.MaxDrive), TOLERANCE);
            Assert.AreEqual(MAX_BRAKE, DefaultOf(configuration, PlayerStatType.MaxBrake), TOLERANCE);
            Assert.AreEqual(JUMP_HEIGHT, DefaultOf(configuration, PlayerStatType.JumpHeight), TOLERANCE);
            Assert.AreEqual(JUMP_COOLDOWN, DefaultOf(configuration, PlayerStatType.JumpCooldown), TOLERANCE);
            Assert.AreEqual(COYOTE_TIME, DefaultOf(configuration, PlayerStatType.CoyoteTime), TOLERANCE);
            Assert.AreEqual(JUMP_BUFFER, DefaultOf(configuration, PlayerStatType.JumpBuffer), TOLERANCE);
            Assert.AreEqual(RISE_GRAVITY, DefaultOf(configuration, PlayerStatType.RiseGravity), TOLERANCE);
            Assert.AreEqual(FALL_GRAVITY, DefaultOf(configuration, PlayerStatType.FallGravity), TOLERANCE);
            Assert.AreEqual(SHORT_HOP_GRAVITY, DefaultOf(configuration, PlayerStatType.ShortHopGravity), TOLERANCE);
            Assert.AreEqual(EXTRA_AIR_JUMPS, DefaultOf(configuration, PlayerStatType.ExtraAirJumps), TOLERANCE);
            Assert.AreEqual(LOOK_TURN_RATE, DefaultOf(configuration, PlayerStatType.LookTurnRate), TOLERANCE);
        }

        [Test]
        public void WhenSprintModifierAdded_ThenFullSpeedIncludesPercent() {
            PlayerStatsConfiguration configuration = AssetDatabase.LoadAssetAtPath<PlayerStatsConfiguration>(ASSET_PATH);
            DiContainer container = new DiContainer();
            container.Bind<PlayerStatsConfiguration>().FromInstance(configuration).AsSingle();
            container.Bind<IStatFactory<PlayerStatType>>().To<PlayerStatFactory>().AsSingle();
            container.Bind<IStatEntityFactory<PlayerStatType>>().To<PlayerStatEntityFactory>().AsSingle();
            GameObject host = new GameObject(nameof(PlayerStatDefaultsTests));
            try {
                PlayerStatEntity entity = host.AddComponent<PlayerStatEntity>();
                container.InjectGameObject(host);
                RunAwake(entity);
                Assert.AreEqual(SPRINT_SPEED, entity.Read(PlayerStatType.SprintSpeed), TOLERANCE);
                entity.AddModifier(PlayerStatType.SprintSpeed, new StatModifier(SPRINT_SPEED_BONUS, ModifierType.PercentAdd));
                Assert.AreEqual(SPRINT_SPEED_WITH_BONUS, entity.Read(PlayerStatType.SprintSpeed), TOLERANCE);
                Assert.AreEqual(WALK_SPEED, entity.Read(PlayerStatType.WalkSpeed), TOLERANCE);
            }
            finally {
                Object.DestroyImmediate(host);
            }
        }

        private static void RunAwake(PlayerStatEntity entity) =>
            typeof(PlayerStatEntity)
                .GetMethod(AWAKE_METHOD, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(entity, null);

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
