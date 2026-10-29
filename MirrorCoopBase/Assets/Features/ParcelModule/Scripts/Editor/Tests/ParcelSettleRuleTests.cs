using Features.ParcelModule.Data;
using Features.ParcelModule.Scripts.Rules;
using NUnit.Framework;

namespace Features.ParcelModule.Scripts.Editor.Tests {
    // At a station a parcel is delivered only at its destination, only lying on board; anything else there is lost,
    // a parcel for a later station waits.
    public sealed class ParcelSettleRuleTests {
        private const uint NET_ID = 7;
        private const long REWARD = 400;
        private const int DESTINATION = 3;

        private readonly ParcelSettleRule _rule = new();

        [Test]
        public void WhenTheShipArrivesAtTheDestination_AndTheParcelLiesOnBoard_ThenItIsDelivered() =>
            Assert.AreEqual(ParcelSettlement.Deliver, _rule.Decide(CreateEntry(ParcelState.Loose), DESTINATION, true));

        [Test]
        public void WhenTheShipArrivesAtTheDestination_AndTheParcelIsNotOnBoard_ThenItIsLost() =>
            Assert.AreEqual(ParcelSettlement.Lose, _rule.Decide(CreateEntry(ParcelState.Loose), DESTINATION, false));

        [TestCase(ParcelState.Carried)]
        [TestCase(ParcelState.Drifting)]
        [TestCase(ParcelState.Pulled)]
        public void WhenTheShipArrivesAtTheDestination_AndTheParcelDoesNotLieLoose_ThenItIsLost(ParcelState state) =>
            Assert.AreEqual(ParcelSettlement.Lose, _rule.Decide(CreateEntry(state), DESTINATION, true));

        [Test]
        public void WhenTheShipStandsAtAnEarlierStation_ThenTheParcelWaits() =>
            Assert.AreEqual(ParcelSettlement.Keep, _rule.Decide(CreateEntry(ParcelState.Loose), DESTINATION - 1, true));

        [Test]
        public void WhenTheShipStandsPastTheDestination_ThenTheParcelIsLost() =>
            Assert.AreEqual(ParcelSettlement.Lose, _rule.Decide(CreateEntry(ParcelState.Loose), DESTINATION + 1, true));

        private static ParcelEntry CreateEntry(ParcelState state) =>
            new(NET_ID, REWARD, DESTINATION, state, 0, string.Empty, string.Empty);
    }
}
