using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.TurretModule.Scripts.Editor.Tests {
    // The seated owner's mouse input turned into turret yaw and pitch within the turret's limits.
    public sealed class TurretAimTests {
        private const float SENSITIVITY = 0.15f;
        private const float MIN_PITCH = -10f;
        private const float MAX_PITCH = 75f;
        private const float FREE_YAW = 180f;
        private const float LIMITED_YAW = 90f;
        private const float TOLERANCE = 0.001f;

        private ShipTurretDefinition _definition;

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_definition);

        [Test]
        public void WhenMouseMovesRightAndUp_ThenTurretTurnsRightAndUpBySensitivity() {
            TurretAim aim = CreateAim(FREE_YAW);

            aim.Turn(new Vector2(100f, 50f));

            Assert.AreEqual(100f * SENSITIVITY, aim.Yaw, TOLERANCE);
            Assert.AreEqual(50f * SENSITIVITY, aim.Pitch, TOLERANCE);
        }

        [Test]
        public void WhenMouseTurnsPastThePitchLimits_ThenPitchStopsAtThem() {
            TurretAim aim = CreateAim(FREE_YAW);

            aim.Turn(new Vector2(0f, 10000f));
            Assert.AreEqual(MAX_PITCH, aim.Pitch, TOLERANCE);

            aim.Turn(new Vector2(0f, -10000f));
            Assert.AreEqual(MIN_PITCH, aim.Pitch, TOLERANCE);
        }

        [Test]
        public void WhenYawIsLimited_AndMouseTurnsFar_ThenYawStopsAtTheLimit() {
            TurretAim aim = CreateAim(LIMITED_YAW);

            aim.Turn(new Vector2(-5000f, 0f));

            Assert.AreEqual(-LIMITED_YAW, aim.Yaw, TOLERANCE);
        }

        [Test]
        public void WhenYawIsFree_AndMouseTurnsPastHalfATurn_ThenYawWrapsAround() {
            TurretAim aim = CreateAim(FREE_YAW);

            aim.Set(170f, 0f);
            aim.Turn(new Vector2(20f / SENSITIVITY, 0f));

            Assert.AreEqual(-170f, aim.Yaw, TOLERANCE);
        }

        [Test]
        public void WhenAimIsSetFromItsOwnDirection_ThenYawAndPitchComeBack() {
            TurretAim sent = CreateAim(FREE_YAW);
            TurretAim received = new TurretAim(_definition);
            sent.Set(-35f, 40f);

            received.SetDirection(sent.Direction);

            Assert.AreEqual(sent.Yaw, received.Yaw, TOLERANCE);
            Assert.AreEqual(sent.Pitch, received.Pitch, TOLERANCE);
        }

        private TurretAim CreateAim(float yawLimit) {
            _definition = ScriptableObject.CreateInstance<ShipTurretDefinition>();
            SerializedObject serialized = new SerializedObject(_definition);
            serialized.FindProperty("_yawLimitDegrees").floatValue = yawLimit;
            serialized.FindProperty("_minPitchDegrees").floatValue = MIN_PITCH;
            serialized.FindProperty("_maxPitchDegrees").floatValue = MAX_PITCH;
            serialized.FindProperty("_lookSensitivity").floatValue = SENSITIVITY;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return new TurretAim(_definition);
        }
    }
}
