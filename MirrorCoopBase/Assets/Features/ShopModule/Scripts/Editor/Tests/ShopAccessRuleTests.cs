using Features.ShopModule.Scripts.Core;
using NUnit.Framework;

namespace Features.ShopModule.Editor.Tests {
    public sealed class ShopAccessRuleTests {
        private const float MAX_DISTANCE = 5f;
        private const float NEAR = 1f;
        private const float FAR = 9f;

        private readonly ShopAccessRule _rule = new ShopAccessRule();

        [Test]
        public void WhenAliveNearInBuild_ThenAllowed() =>
            Assert.That(_rule.Evaluate(true, true, NEAR, MAX_DISTANCE), Is.EqualTo(ShopAccessStatus.Allowed));

        [Test]
        public void WhenExactlyAtMaxDistance_ThenAllowed() =>
            Assert.That(_rule.Evaluate(true, true, MAX_DISTANCE, MAX_DISTANCE), Is.EqualTo(ShopAccessStatus.Allowed));

        [Test]
        public void WhenNotInBuild_ThenNotBuildPhase() =>
            Assert.That(_rule.Evaluate(false, true, NEAR, MAX_DISTANCE), Is.EqualTo(ShopAccessStatus.NotBuildPhase));

        [Test]
        public void WhenDead_ThenDead() =>
            Assert.That(_rule.Evaluate(true, false, NEAR, MAX_DISTANCE), Is.EqualTo(ShopAccessStatus.Dead));

        [Test]
        public void WhenFar_ThenTooFar() =>
            Assert.That(_rule.Evaluate(true, true, FAR, MAX_DISTANCE), Is.EqualTo(ShopAccessStatus.TooFar));

        [Test]
        public void WhenDeadFarInFlight_ThenPhaseIsReportedFirst() =>
            Assert.That(_rule.Evaluate(false, false, FAR, MAX_DISTANCE), Is.EqualTo(ShopAccessStatus.NotBuildPhase));

        [Test]
        public void WhenDeadAndFarInBuild_ThenDeadIsReportedFirst() =>
            Assert.That(_rule.Evaluate(true, false, FAR, MAX_DISTANCE), Is.EqualTo(ShopAccessStatus.Dead));

        [TestCase(5f, 6f, 7f)]
        [TestCase(5f, 6f, 6f)]
        public void WhenDistancesGrowFromOpenToPurchase_ThenOrderIsValid(float open, float keepOpen, float purchase) =>
            Assert.That(_rule.IsDistanceOrderValid(open, keepOpen, purchase), Is.True);

        [TestCase(6f, 6f, 7f)]
        [TestCase(7f, 6f, 8f)]
        [TestCase(5f, 7f, 6f)]
        public void WhenKeepOpenIsNotAboveOpenOrPurchaseIsBelowKeepOpen_ThenOrderIsInvalid(float open, float keepOpen, float purchase) =>
            Assert.That(_rule.IsDistanceOrderValid(open, keepOpen, purchase), Is.False);
    }
}
