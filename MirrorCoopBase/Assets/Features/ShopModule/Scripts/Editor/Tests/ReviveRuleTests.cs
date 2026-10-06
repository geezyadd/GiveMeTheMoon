using Features.ShopModule.Scripts.Core;
using NUnit.Framework;

namespace Features.ShopModule.Editor.Tests {
    // The buyer side goes through the real ShopAccessRule, as on the server: a buy-back uses the purchase kiosk check.
    public sealed class ReviveRuleTests {
        private const float MAX_DISTANCE = 7f;
        private const float NEAR = 1f;
        private const float FAR = 9f;
        private const long PRICE = 300;
        private const long RICH = 1000;
        private const long POOR = 299;

        private readonly ShopAccessRule _shopAccessRule = new ShopAccessRule();
        private readonly ReviveRule _rule = new ReviveRule();

        [Test]
        public void WhenAliveBuyerNearInBuildRevivesDeadOnlineTargetWithMoney_ThenAllowed() =>
            Assert.That(Evaluate(), Is.EqualTo(ReviveStatus.Allowed));

        [Test]
        public void WhenBalanceEqualsPrice_ThenAllowed() =>
            Assert.That(Evaluate(balance: PRICE), Is.EqualTo(ReviveStatus.Allowed));

        [Test]
        public void WhenNotInBuild_ThenNotBuildPhase() =>
            Assert.That(Evaluate(isBuildPhase: false), Is.EqualTo(ReviveStatus.NotBuildPhase));

        [Test]
        public void WhenBuyerDead_ThenBuyerDead() =>
            Assert.That(Evaluate(isBuyerAlive: false), Is.EqualTo(ReviveStatus.BuyerDead));

        [Test]
        public void WhenBuyerFarFromKiosk_ThenTooFar() =>
            Assert.That(Evaluate(distance: FAR), Is.EqualTo(ReviveStatus.TooFar));

        [Test]
        public void WhenTargetOffline_ThenTargetOffline() =>
            Assert.That(Evaluate(isTargetOnline: false), Is.EqualTo(ReviveStatus.TargetOffline));

        [Test]
        public void WhenTargetAlive_ThenTargetNotDead() =>
            Assert.That(Evaluate(isTargetDead: false), Is.EqualTo(ReviveStatus.TargetNotDead));

        [Test]
        public void WhenNotEnoughMoney_ThenNotEnoughMoney() =>
            Assert.That(Evaluate(balance: POOR), Is.EqualTo(ReviveStatus.NotEnoughMoney));

        [Test]
        public void WhenInFlightWithDeadTargetAndMoney_ThenNotBuildPhase() =>
            Assert.That(Evaluate(isBuildPhase: false, distance: FAR), Is.EqualTo(ReviveStatus.NotBuildPhase));

        [Test]
        public void WhenTargetWasRevivedByAnotherBuyerFirst_ThenSecondBuyBackIsRefused() =>
            Assert.That(Evaluate(isTargetDead: false, balance: RICH - PRICE), Is.EqualTo(ReviveStatus.TargetNotDead));

        [Test]
        public void WhenBuyerDeadAndPoor_ThenBuyerDeadIsReportedFirst() =>
            Assert.That(Evaluate(isBuyerAlive: false, balance: POOR), Is.EqualTo(ReviveStatus.BuyerDead));

        [TestCase(PRICE, true)]
        [TestCase(RICH, true)]
        [TestCase(POOR, false)]
        [TestCase(0L, false)]
        public void WhenComparingBalanceWithPrice_ThenAffordableOnlyFromThePrice(long balance, bool isAffordable) =>
            Assert.That(_rule.CanAfford(balance, PRICE), Is.EqualTo(isAffordable));

        [TestCase(1L, true)]
        [TestCase(PRICE, true)]
        [TestCase(0L, false)]
        [TestCase(-1L, false)]
        public void WhenCheckingConfiguredPrice_ThenOnlyAPositivePriceIsValid(long price, bool isValid) =>
            Assert.That(_rule.IsPriceValid(price), Is.EqualTo(isValid));

        private ReviveStatus Evaluate(
            bool isBuildPhase = true,
            bool isBuyerAlive = true,
            float distance = NEAR,
            bool isTargetOnline = true,
            bool isTargetDead = true,
            long balance = RICH) =>
            _rule.Evaluate(new ReviveRequest(
                _shopAccessRule.Evaluate(isBuildPhase, isBuyerAlive, distance, MAX_DISTANCE),
                isTargetOnline,
                isTargetDead,
                balance,
                PRICE));
    }
}
