using Features.SonarModule.Scripts.Core;
using NUnit.Framework;
using UnityEngine;

namespace Features.SonarModule.Editor.Tests {
    public sealed class SonarConsoleRuleTests {
        private const string FIRST_PLAYER = "client:first";
        private const string SECOND_PLAYER = "client:second";

        private SonarTestConfiguration _configuration;
        private SonarConsoleRule _rule;

        [SetUp]
        public void SetUp() {
            _configuration = new SonarTestConfiguration();
            _rule = new SonarConsoleRule(_configuration.Configuration);
        }

        [TearDown]
        public void TearDown() =>
            _configuration.Dispose();

        [Test]
        public void WhenTheConsoleIsFree_ThenThePlayerBecomesTheNavigator() =>
            Assert.AreEqual(SonarConsoleEnterResult.Entered, _rule.EvaluateEnter(string.Empty, FIRST_PLAYER, true));

        [Test]
        public void WhenAnotherPlayerNavigates_ThenTheConsoleIsBusy() =>
            Assert.AreEqual(SonarConsoleEnterResult.Busy, _rule.EvaluateEnter(FIRST_PLAYER, SECOND_PLAYER, true));

        [Test]
        public void WhenTheNavigatorUsesTheConsoleAgain_ThenTheyStayTheNavigator() =>
            Assert.AreEqual(SonarConsoleEnterResult.AlreadyNavigator, _rule.EvaluateEnter(FIRST_PLAYER, FIRST_PLAYER, true));

        [Test]
        public void WhenTheConsoleIsInactive_ThenNobodyEnters() =>
            Assert.AreEqual(SonarConsoleEnterResult.Inactive, _rule.EvaluateEnter(string.Empty, FIRST_PLAYER, false));

        [Test]
        public void WhenThePlayerWalksAway_ThenTheyLeaveTheConsoleReach() {
            Assert.IsTrue(_rule.IsWithinReach(new Vector3(0f, 0f, SonarTestConfiguration.KEEP_OPEN_METERS - 0.1f), Vector3.zero));
            Assert.IsFalse(_rule.IsWithinReach(new Vector3(0f, 0f, SonarTestConfiguration.KEEP_OPEN_METERS + 0.1f), Vector3.zero));
        }
    }
}
