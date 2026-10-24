using System;
using Features.FlightFieldModule.Contracts;
using Features.NetworkModelModule.Scripts;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Systems;
using NUnit.Framework;
using UnityEngine;

namespace Features.ShopModule.Editor.Tests {
    // A popped target balloon pays its reward into the bound crew wallet once per pop event; nothing is paid after the
    // system is disposed; a pop with no wallet bound is a broken server and throws.
    public sealed class FieldTargetRewardSystemTests {
        private const long STARTING_BALANCE = 300;
        private const int SMALL_REWARD = 20;
        private const int GOLDEN_REWARD = 200;

        private TargetEventsStub _events;
        private WalletStub _wallet;
        private CrewWalletBindingModel _binding;
        private FieldTargetRewardSystem _system;

        [SetUp]
        public void SetUp() {
            _events = new TargetEventsStub();
            _wallet = new WalletStub(STARTING_BALANCE);
            _binding = new CrewWalletBindingModel();
            _binding.Bind(_wallet);
            _system = new FieldTargetRewardSystem(_events, _binding);
            _system.Initialize();
        }

        [TearDown]
        public void TearDown() =>
            _system.Dispose();

        [Test]
        public void WhenBalloonsPop_ThenTheCrewIsPaidEachRewardOnce() {
            _events.Pop(0, SMALL_REWARD);
            _events.Pop(1, GOLDEN_REWARD);

            Assert.AreEqual(STARTING_BALANCE + SMALL_REWARD + GOLDEN_REWARD, _wallet.Balance);
            Assert.AreEqual(2, _wallet.Earnings, "Every pop must pay exactly once.");
        }

        [Test]
        public void WhenTheSystemIsDisposed_ThenAPopPaysNothing() {
            _system.Dispose();
            _events.Pop(0, SMALL_REWARD);

            Assert.AreEqual(STARTING_BALANCE, _wallet.Balance);
        }

        [Test]
        public void WhenNoWalletIsBound_ThenAPopThrows() {
            _binding.Clear(_wallet);

            Assert.Throws<InvalidOperationException>(() => _events.Pop(0, SMALL_REWARD));
        }

        private sealed class TargetEventsStub : IFieldTargetEvents {
            public event Action<FieldTargetPopped> TargetPopped;

            public void Pop(int index, int reward) {
                FieldTargetDescription target = new(new FieldTargetId(index), "balloon", Vector3.zero, 1f, reward);
                TargetPopped?.Invoke(new FieldTargetPopped(target, FieldTargetPopCause.Weapon, new PlayerKey("client:shooter"), "minigun", 1, 0));
            }
        }

        private sealed class WalletStub : ICrewWallet {
            public long Balance { get; private set; }
            public int Earnings { get; private set; }

            public WalletStub(long balance) =>
                Balance = balance;

            public bool ServerTrySpend(long amount) =>
                throw new NotSupportedException();

            public void ServerEarn(long amount) {
                Balance += amount;
                Earnings++;
            }
        }
    }
}
