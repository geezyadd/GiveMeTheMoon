using NUnit.Framework;

namespace Features.CharacterMovableModule.Scripts.Editor {
    public sealed class JumpAndAirGravityPressTests {
        private const float FRAME = 0.02f;
        private const float GRAVITY = -9.81f;
        private const float RISE_GRAVITY = 1.2f;
        private const float SHORT_HOP_GRAVITY = 2f;
        private const float RISING_SPEED = 5f;

        private static readonly JumpTuning NoAirJumps = Tuning(0);
        private static readonly JumpTuning OneAirJump = Tuning(1);

        private JumpAndAirGravity _jump;

        [SetUp]
        public void SetUp() =>
            _jump = new JumpAndAirGravity();

        [Test]
        public void WhenAPressIsGivenOnTheGround_ThenTheBodyTakesOff() {
            _jump.ReadInput(true, true, true, FRAME);

            Assert.IsTrue(TryTakeoff(NoAirJumps, true));
        }

        [Test]
        public void WhenTheKeyIsHeldWithoutAPress_ThenThereIsNoTakeoff() {
            _jump.ReadInput(true, false, true, FRAME);

            Assert.IsFalse(TryTakeoff(NoAirJumps, true));
        }

        [Test]
        public void WhenAPressComesWithoutTheHeldState_ThenTheBodyStillJumpsWithTheShortHopGravity() {
            _jump.ReadInput(false, true, true, FRAME);

            Assert.IsTrue(TryTakeoff(NoAirJumps, true));
            Assert.AreEqual(SHORT_HOP_GRAVITY, _jump.AirGravityExtra(NoAirJumps, RISING_SPEED));
        }

        [Test]
        public void WhenTheKeyStaysDown_ThenTheEdgeReadingGivesOnlyOneJump() {
            _jump.ReadInput(true, true, FRAME);
            Assert.IsTrue(TryTakeoff(NoAirJumps, true));
            Assert.AreEqual(RISE_GRAVITY, _jump.AirGravityExtra(NoAirJumps, RISING_SPEED));

            for (int frame = 0; frame < 50; frame++) {
                _jump.ReadInput(true, true, FRAME);
                Assert.IsFalse(TryTakeoff(NoAirJumps, true), $"jumped again at frame {frame} without a new press");
            }
        }

        [Test]
        public void WhenAPressComesInTheAirWithoutAirJumps_ThenThereIsNoTakeoff() {
            _jump.ReadInput(true, true, false, FRAME);

            Assert.IsFalse(TryTakeoff(NoAirJumps, false));
        }

        [Test]
        public void WhenAPressComesInTheAirWithAnAirJumpLeft_ThenTheBodyTakesOff() {
            _jump.RefillAirJumps(OneAirJump.ExtraAirJumps);
            _jump.ReadInput(true, true, false, FRAME);

            Assert.IsTrue(TryTakeoff(OneAirJump, false));
        }

        private bool TryTakeoff(JumpTuning tuning, bool supported) =>
            _jump.TryTakeoff(tuning, supported, 0f, GRAVITY, FRAME, out _);

        private static JumpTuning Tuning(int extraAirJumps) =>
            new(
                jumpHeight: 4f,
                jumpCooldown: 0.2f,
                coyoteTime: 0.2f,
                jumpBuffer: 0.2f,
                riseGravity: RISE_GRAVITY,
                shortHopGravity: SHORT_HOP_GRAVITY,
                fallGravity: 3f,
                extraAirJumps: extraAirJumps);
    }
}
