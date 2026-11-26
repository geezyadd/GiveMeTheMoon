using Features.CharacterMovableModule.Scripts.Drift;
using NUnit.Framework;
using UnityEngine;

namespace Features.CharacterMovableModule.Editor.Tests {
    public sealed class CharacterDriftHeadFirstTurnTests {
        private const float STEP_SECONDS = 0.02f;
        private const float TOLERANCE_DEGREES = 1f;

        private readonly CharacterDriftSettings _settings = new CharacterDriftSettings();

        [Test]
        public void WhenBodyFliesLong_ThenItsHeadPointsAlongTheFlight() {
            CharacterDriftHeadFirstTurn turn = new CharacterDriftHeadFirstTurn();
            Vector3 velocity = new Vector3(3f, 0f, 2f);

            Run(turn, true, velocity, 10f);

            Assert.AreEqual(0f, Vector3.Angle(turn.Rotation * Vector3.up, velocity), TOLERANCE_DEGREES);
        }

        [Test]
        public void WhenBodyStartsFlying_ThenItTurnsSmoothlyNotAtOnce() {
            CharacterDriftHeadFirstTurn turn = new CharacterDriftHeadFirstTurn();

            turn.Step(true, Vector3.forward * 3f, _settings, STEP_SECONDS);

            float turned = Vector3.Angle(turn.Rotation * Vector3.up, Vector3.up);
            Assert.Greater(turned, 0f);
            Assert.Less(turned, 10f);
        }

        [Test]
        public void WhenBodyAlmostStops_ThenItKeepsItsTurn() {
            CharacterDriftHeadFirstTurn turn = new CharacterDriftHeadFirstTurn();
            Run(turn, true, Vector3.forward * 3f, 10f);
            Quaternion before = turn.Rotation;

            Run(turn, true, Vector3.forward * 0.01f, 2f);

            Assert.AreEqual(0f, Quaternion.Angle(before, turn.Rotation), TOLERANCE_DEGREES);
        }

        [Test]
        public void WhenBodyNoLongerFlies_ThenItStandsUprightAgain() {
            CharacterDriftHeadFirstTurn turn = new CharacterDriftHeadFirstTurn();
            Run(turn, true, Vector3.forward * 3f, 10f);

            Run(turn, false, Vector3.forward * 3f, 10f);

            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, turn.Rotation), TOLERANCE_DEGREES);
        }

        private void Run(CharacterDriftHeadFirstTurn turn, bool isFlying, Vector3 velocity, float seconds) {
            for (float time = 0f; time < seconds; time += STEP_SECONDS)
                turn.Step(isFlying, velocity, _settings, STEP_SECONDS);
        }
    }
}
