using NUnit.Framework;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipSocketRuleTests {
        private const int UNLOCK_LOOP = 1;

        private ShipSocketRule _engineRule;

        [SetUp]
        public void SetUp() =>
            _engineRule = new ShipSocketRule(ShipModuleType.Engine, UNLOCK_LOOP);

        [Test]
        public void WhenEmptyOpenSocketInBuild_ThenModuleCanBeInstalled() =>
            Assert.IsTrue(_engineRule.CanInstall(false, ShipModuleType.Engine, Run(ShipRunPhase.Build)));

        [TestCase(ShipRunPhase.Takeoff)]
        [TestCase(ShipRunPhase.Cruise)]
        [TestCase(ShipRunPhase.Landing)]
        [TestCase(ShipRunPhase.Wreck)]
        public void WhenNotInBuild_ThenModuleCannotBeInstalled(ShipRunPhase phase) =>
            Assert.IsFalse(_engineRule.CanInstall(false, ShipModuleType.Engine, Run(phase)));

        [Test]
        public void WhenSocketIsOccupied_ThenModuleCannotBeInstalled() =>
            Assert.IsFalse(_engineRule.CanInstall(true, ShipModuleType.Engine, Run(ShipRunPhase.Build)));

        [Test]
        public void WhenModuleTypeDoesNotMatch_ThenModuleCannotBeInstalled() =>
            Assert.IsFalse(_engineRule.CanInstall(false, ShipModuleType.Radar, Run(ShipRunPhase.Build)));

        [Test]
        public void WhenSocketIsLocked_ThenModuleCannotBeInstalled() =>
            Assert.IsFalse(_engineRule.CanInstall(false, ShipModuleType.Engine, Run(ShipRunPhase.Build, UNLOCK_LOOP - 1)));

        [Test]
        public void WhenOccupiedEngineSocketInBuild_ThenModuleCanBeUninstalled() =>
            Assert.IsTrue(_engineRule.CanUninstall(true, Run(ShipRunPhase.Build)));

        [TestCase(ShipRunPhase.Takeoff)]
        [TestCase(ShipRunPhase.Cruise)]
        [TestCase(ShipRunPhase.Landing)]
        [TestCase(ShipRunPhase.Wreck)]
        public void WhenNotInBuild_ThenModuleCannotBeUninstalled(ShipRunPhase phase) =>
            Assert.IsFalse(_engineRule.CanUninstall(true, Run(phase)));

        [Test]
        public void WhenSocketIsEmpty_ThenNothingCanBeUninstalled() =>
            Assert.IsFalse(_engineRule.CanUninstall(false, Run(ShipRunPhase.Build)));

        [TestCase(ShipModuleType.Control)]
        [TestCase(ShipModuleType.Seat)]
        [TestCase(ShipModuleType.Turret)]
        public void WhenModuleTypeIsNotRemovable_ThenModuleCannotBeUninstalled(ShipModuleType type) =>
            Assert.IsFalse(new ShipSocketRule(type, UNLOCK_LOOP).CanUninstall(true, Run(ShipRunPhase.Build)));

        [Test]
        public void WhenRadarSocketIsOccupiedInBuild_ThenModuleCanBeUninstalled() =>
            Assert.IsTrue(new ShipSocketRule(ShipModuleType.Radar, UNLOCK_LOOP).CanUninstall(true, Run(ShipRunPhase.Build)));

        [Test]
        public void WhenLoopReachesUnlockLoop_ThenSocketIsUnlocked() =>
            Assert.IsTrue(_engineRule.IsUnlocked(Run(ShipRunPhase.Cruise, UNLOCK_LOOP)));

        private static ShipSocketRunState Run(ShipRunPhase phase, int loopIndex = UNLOCK_LOOP) =>
            new(loopIndex, phase);
    }
}
