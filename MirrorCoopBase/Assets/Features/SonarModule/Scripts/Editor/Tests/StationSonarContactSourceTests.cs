using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Features.SonarModule.Contracts;
using Features.SonarModule.Scripts.Core;
using Features.SonarModule.Scripts.Sources;
using NUnit.Framework;
using UnityEngine;

namespace Features.SonarModule.Editor.Tests {
    public sealed class StationSonarContactSourceTests {
        private const float RANGE = 500f;
        private const string STATION_ID = "station";

        private static readonly Vector3 _destination = new Vector3(400f, 100f, 5000f);

        private SonarTestConfiguration _configuration;
        private FakeShipRunModel _run;
        private StationSonarContactSource _source;
        private readonly List<SonarContact> _contacts = new();

        [SetUp]
        public void SetUp() {
            _configuration = new SonarTestConfiguration();
            _run = new FakeShipRunModel { Phase = ShipRunPhase.Cruise, TransitDestination = _destination };
            _source = new StationSonarContactSource(_run, new SonarFlightRule(), _configuration.Configuration);
            _contacts.Clear();
        }

        [TearDown]
        public void TearDown() =>
            _configuration.Dispose();

        [Test]
        public void WhenTheShipCruisesFarFromTheStation_ThenTheStationIsStillAContact() {
            _source.CollectContacts(Vector3.zero, RANGE, _contacts);

            Assert.AreEqual(1, _contacts.Count);
            Assert.AreSame(_configuration.StationKind, _contacts[0].Kind);
            Assert.AreEqual(_destination, _contacts[0].Position);
            Assert.IsTrue(_contacts[0].Kind.IsShownBeyondRange);
        }

        [Test]
        public void WhenTheStationIsLookedUpById_ThenItIsTheDestination() {
            Assert.IsTrue(_source.TryGetContact(STATION_ID, out SonarContact station));
            Assert.AreEqual(_destination, station.Position);
            Assert.IsFalse(_source.TryGetContact("rock:c:0:0", out _));
        }

        [Test]
        public void WhenTheShipStandsAtAStation_ThenThereIsNoStationContact() {
            _run.Phase = ShipRunPhase.Build;

            _source.CollectContacts(Vector3.zero, RANGE, _contacts);

            Assert.AreEqual(0, _contacts.Count);
            Assert.IsFalse(_source.TryGetContact(STATION_ID, out _));
        }
    }
}
