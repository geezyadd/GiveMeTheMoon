using System;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Systems;
using NUnit.Framework;

namespace Features.ShopModule.Editor.Tests {
    // Ore that reached the ship pays its price into the bound crew wallet once per delivery event; nothing is paid
    // after the system is disposed; a delivery with no wallet bound is a broken server and throws.
    public sealed class FieldOreRewardSystemTests {
        private const long STARTING_BALANCE = 300;
        private const int SHARD_PRICE = 12;
        private const int CORE_PRICE = 60;

        private OreEventsStub _events;
        private WalletStub _wallet;
        private CrewWalletBindingModel _binding;
        private FieldOreRewardSystem _system;

        [SetUp]
        public void SetUp() {
            _events = new OreEventsStub();
            _wallet = new WalletStub(STARTING_BALANCE);
            _binding = new CrewWalletBindingModel();
            _binding.Bind(_wallet);
            _system = new FieldOreRewardSystem(_events, _binding);
            _system.Initialize();
        }

        [TearDown]
        public void TearDown() =>
            _system.Dispose();

        [Test]
        public void WhenOreReachesTheShip_ThenTheCrewIsPaidEachPriceOnce() {
            _events.Deliver(1, SHARD_PRICE);
            _events.Deliver(2, CORE_PRICE);

            Assert.AreEqual(STARTING_BALANCE + SHARD_PRICE + CORE_PRICE, _wallet.Balance);
            Assert.AreEqual(2, _wallet.Earnings, "Every delivery must pay exactly once.");
        }

        [Test]
        public void WhenTheSystemIsDisposed_ThenADeliveryPaysNothing() {
            _system.Dispose();
            _events.Deliver(1, SHARD_PRICE);

            Assert.AreEqual(STARTING_BALANCE, _wallet.Balance);
        }

        [Test]
        public void WhenNoWalletIsBound_ThenADeliveryThrows() {
            _binding.Clear(_wallet);

            Assert.Throws<InvalidOperationException>(() => _events.Deliver(1, SHARD_PRICE));
        }
    }
}
