using Features.SonarModule.Scripts.Core;
using NUnit.Framework;

namespace Features.SonarModule.Editor.Tests {
    public sealed class SonarMarkRuleTests {
        private SonarTestConfiguration _configuration;
        private SonarMarkRule _rule;

        [SetUp]
        public void SetUp() {
            _configuration = new SonarTestConfiguration();
            _rule = new SonarMarkRule(_configuration.Configuration);
        }

        [TearDown]
        public void TearDown() =>
            _configuration.Dispose();

        [Test]
        public void WhenNavigatorMarksKnownContactBelowLimit_ThenItIsMarked() =>
            Assert.AreEqual(SonarMarkResult.Marked, _rule.Evaluate(new SonarMarkRequest(true, true, true, false, 0)));

        [Test]
        public void WhenNotTheNavigatorMarks_ThenTheMarkIsRefused() =>
            Assert.AreEqual(SonarMarkResult.NotNavigator, _rule.Evaluate(new SonarMarkRequest(false, true, true, false, 0)));

        [Test]
        public void WhenTheContactDoesNotExist_ThenTheMarkIsRefused() =>
            Assert.AreEqual(SonarMarkResult.UnknownContact, _rule.Evaluate(new SonarMarkRequest(true, false, false, false, 0)));

        [Test]
        public void WhenTheKindIsNotMarkable_ThenTheMarkIsRefused() =>
            Assert.AreEqual(SonarMarkResult.NotMarkable, _rule.Evaluate(new SonarMarkRequest(true, true, false, false, 0)));

        [Test]
        public void WhenTheLimitIsReached_ThenANewMarkIsRefused() =>
            Assert.AreEqual(SonarMarkResult.LimitReached, _rule.Evaluate(new SonarMarkRequest(true, true, true, false, SonarTestConfiguration.MARK_LIMIT)));

        [Test]
        public void WhenAMarkedContactIsMarkedAgainAtTheLimit_ThenItsMarkIsRenewed() =>
            Assert.AreEqual(SonarMarkResult.Refreshed, _rule.Evaluate(new SonarMarkRequest(true, true, true, true, SonarTestConfiguration.MARK_LIMIT)));

        [TestCase(9.99, false)]
        [TestCase(10.0, true)]
        [TestCase(12.0, true)]
        public void WhenTimePasses_ThenAMarkExpiresAtItsEnd(double now, bool isExpired) =>
            Assert.AreEqual(isExpired, _rule.IsExpired(10.0, now));
    }
}
