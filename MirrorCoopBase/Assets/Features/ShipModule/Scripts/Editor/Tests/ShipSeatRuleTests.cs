using Features.NetworkModelModule.Scripts;
using Features.ShipModule.Scripts;
using NUnit.Framework;

namespace Features.ShipModule.Editor.Tests {
    // Seat occupancy by the player's stable key: one occupant, only the occupant stands up, and the key survives a
    // rejoin while the netId does not.
    public sealed class ShipSeatRuleTests {
        private static readonly PlayerKey _noOne = new PlayerKey(string.Empty);
        private static readonly PlayerKey _player = new PlayerKey("client:player");
        private static readonly PlayerKey _otherPlayer = new PlayerKey("client:other");

        private ShipSeatRule _rule;

        [SetUp]
        public void SetUp() =>
            _rule = new ShipSeatRule();

        [Test]
        public void WhenSeatIsFree_ThenPlayerCanSit() =>
            Assert.IsTrue(_rule.CanSit(true, _noOne, _player));

        [Test]
        public void WhenSeatIsTaken_ThenAnotherPlayerCannotSit() =>
            Assert.IsFalse(_rule.CanSit(true, _player, _otherPlayer));

        [Test]
        public void WhenSlotViewHasNoSeat_ThenNobodyCanSit() =>
            Assert.IsFalse(_rule.CanSit(false, _noOne, _player));

        [Test]
        public void WhenPlayerHasNoKey_ThenItCannotSit() =>
            Assert.IsFalse(_rule.CanSit(true, _noOne, _noOne));

        [Test]
        public void WhenPlayerSits_ThenTheSeatIsHeldByItsKeyOnly() {
            Assert.IsTrue(_rule.IsHeldBy(_player, _player));
            Assert.IsFalse(_rule.IsHeldBy(_player, _otherPlayer));
        }

        [Test]
        public void WhenPlayerRejoins_ThenItsNewObjectHasTheSameKey_AndHoldsWhatTheKeyHolds() =>
            Assert.IsTrue(_rule.IsHeldBy(_player, new PlayerKey(_player.Id)));

        [Test]
        public void WhenSeatIsFree_ThenNoKeyHoldsIt() {
            Assert.IsFalse(_rule.IsHeldBy(_noOne, _noOne));
            Assert.IsFalse(_rule.IsHeldBy(new PlayerKey(null), _noOne));
        }

        [Test]
        public void WhenSocketStateHasNoOccupant_ThenItEqualsAFreedSeat() {
            ShipSocketState freed = new ShipSocketState { Occupied = true, ModuleId = "helm", OccupantKey = string.Empty };
            ShipSocketState neverSeated = new ShipSocketState { Occupied = true, ModuleId = "helm" };

            Assert.AreEqual(neverSeated, freed);
        }
    }
}
