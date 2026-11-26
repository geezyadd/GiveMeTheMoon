using NUnit.Framework;

namespace Features.PlayerBodyModule.Editor.Tests {
    public sealed class PlayerAttackPressRuleTests {
        [Test]
        public void WhenNothingStandsInTheWay_ThenThePressCounts() {
            Assert.IsTrue(PlayerAttackPressRule.Allows(State()));
        }

        [Test]
        public void WhenAnItemIsInTheHand_ThenThePressDoesNotCount() {
            Assert.IsFalse(PlayerAttackPressRule.Allows(State(isHolding: true)));
        }

        [Test]
        public void WhenThePlayerIsSeated_ThenThePressDoesNotCount() {
            Assert.IsFalse(PlayerAttackPressRule.Allows(State(isSeated: true)));
        }

        [Test]
        public void WhenTheControlsAreBlocked_ThenThePressDoesNotCount() {
            Assert.IsFalse(PlayerAttackPressRule.Allows(State(isControlBlocked: true)));
        }

        [Test]
        public void WhenTheCursorIsFree_ThenThePressDoesNotCount() {
            Assert.IsFalse(PlayerAttackPressRule.Allows(State(isCursorFree: true)));
        }

        [Test]
        public void WhenAnAttackIsRunning_ThenThePressDoesNotCount() {
            Assert.IsFalse(PlayerAttackPressRule.Allows(State(isAttacking: true)));
        }

        private static PlayerAttackPressRule.State State(bool isHolding = false, bool isSeated = false,
            bool isControlBlocked = false, bool isCursorFree = false, bool isAttacking = false) =>
            new(isHolding, isSeated, isControlBlocked, isCursorFree, isAttacking);
    }
}
