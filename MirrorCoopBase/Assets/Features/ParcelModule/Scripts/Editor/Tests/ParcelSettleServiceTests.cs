using System;
using Features.ParcelModule.Data;
using Features.ParcelModule.Scripts.Crate;
using Features.ParcelModule.Scripts.Rules;
using Features.ParcelModule.Scripts.Server;
using Features.ParcelModule.Scripts.Services;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Features.ParcelModule.Scripts.Editor.Tests {
    // Arriving at a station pays a parcel's reward into the crew wallet exactly once, only at its destination and only
    // with the parcel on board; a parcel not on board is lost there without a fine, and losing everything pays nothing.
    public sealed class ParcelSettleServiceTests {
        private const uint NET_ID = 11;
        private const long REWARD = 450;
        private const int DESTINATION = 2;

        private ParcelServerModel _serverModel;
        private FakeParcelEntryService _entries;
        private FakeParcelCrateService _crates;
        private FakeParcelAboardService _aboard;
        private RecordingWallet _wallet;
        private ParcelSettleService _service;
        private GameObject _crateObject;

        [SetUp]
        public void SetUp() {
            _serverModel = new ParcelServerModel();
            _entries = new FakeParcelEntryService();
            _crates = new FakeParcelCrateService(_serverModel, _entries);
            _aboard = new FakeParcelAboardService();
            _wallet = new RecordingWallet();
            CrewWalletBindingModel binding = new();
            binding.Bind(_wallet);
            _service = new ParcelSettleService(_serverModel, _entries, _crates, _aboard, new ParcelSettleRule(), binding);
            AddParcel();
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_crateObject);

        [Test]
        public void WhenTheShipArrivesAtTheDestination_AndTheParcelIsOnBoard_ThenTheCrewIsPaidOnce() {
            _service.ServerSettleAtStation(DESTINATION, NoShip);
            _service.ServerSettleAtStation(DESTINATION, NoShip);

            Assert.AreEqual(1, _wallet.Earnings);
            Assert.AreEqual(REWARD, _wallet.Balance);
            CollectionAssert.AreEqual(new[] { NET_ID }, _crates.Destroyed);
            Assert.IsEmpty(_entries.Entries);
        }

        [Test]
        public void WhenTheShipStandsAtAStationBeforeTheDestination_ThenNothingIsPaidAndTheParcelStays() {
            _service.ServerSettleAtStation(DESTINATION - 1, NoShip);

            Assert.AreEqual(0, _wallet.Earnings);
            Assert.IsEmpty(_crates.Destroyed);
            Assert.AreEqual(1, _entries.Entries.Count);
        }

        [Test]
        public void WhenTheParcelIsNotOnBoardAtTheDestination_ThenItIsLostWithoutPayOrFine() {
            _aboard.IsEverythingAboard = false;
            _service.ServerSettleAtStation(DESTINATION, NoShip);

            Assert.AreEqual(0, _wallet.Earnings);
            Assert.AreEqual(0, _wallet.Balance);
            CollectionAssert.AreEqual(new[] { NET_ID }, _crates.Destroyed);
        }

        [Test]
        public void WhenEverythingIsLost_ThenNothingIsPaid() {
            _service.ServerLoseAll();

            Assert.AreEqual(0, _wallet.Earnings);
            Assert.IsEmpty(_entries.Entries);
        }

        [Test]
        public void WhenNoWalletIsBound_ThenADeliveryThrows() {
            ParcelSettleService unbound = new(_serverModel, _entries, _crates, _aboard, new ParcelSettleRule(), new CrewWalletBindingModel());
            Assert.Throws<InvalidOperationException>(() => unbound.ServerSettleAtStation(DESTINATION, NoShip));
        }

        // The fake on-board check never asks the ship.
        private static ShipBase NoShip =>
            null;

        private void AddParcel() {
            _crateObject = new GameObject(nameof(ParcelSettleServiceTests));
            Rigidbody body = _crateObject.AddComponent<Rigidbody>();
            ParcelCrate crate = _crateObject.AddComponent<ParcelCrate>();
            SerializedObject serialized = new(crate);
            serialized.FindProperty("_body").objectReferenceValue = body;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _serverModel.Add(new ParcelServerRecord(NET_ID, crate, null, 0f));
            _entries.ServerAdd(new ParcelEntry(NET_ID, REWARD, DESTINATION, ParcelState.Loose, 0, string.Empty, string.Empty));
        }
    }
}
