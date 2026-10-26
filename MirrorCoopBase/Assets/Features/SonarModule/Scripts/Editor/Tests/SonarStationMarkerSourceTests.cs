using Features.ShipModule.Scripts;
using Features.SonarModule.Contracts;
using Features.SonarModule.Data;
using Features.SonarModule.Scripts.Core;
using Features.SonarModule.Scripts.Sources;
using NUnit.Framework;
using UnityEngine;

namespace Features.SonarModule.Editor.Tests {
    public sealed class SonarStationMarkerSourceTests {
        private static readonly Vector3 _destination = new Vector3(400f, 100f, 5000f);

        private SonarTestConfiguration _configuration;
        private FakeShipRunModel _run;
        private SonarStationMarkerModel _markerModel;
        private SonarStationMarkerSource _source;
        private int _changeCount;

        [SetUp]
        public void SetUp() {
            _configuration = new SonarTestConfiguration();
            _run = new FakeShipRunModel { Phase = ShipRunPhase.Cruise, TransitDestination = _destination };
            _markerModel = new SonarStationMarkerModel();
            _source = new SonarStationMarkerSource(_markerModel, new StationSonarContactSource(_run, _configuration.Configuration));
        }

        [TearDown]
        public void TearDown() {
            _markerModel.OnIsShownChanged -= OnIsShownChanged;
            _configuration.Dispose();
        }

        [Test]
        public void WhenTheMarkerIsOff_ThenNoStationIsMarked() =>
            Assert.IsFalse(_source.TryGetStation(out _));

        [Test]
        public void WhenTheMarkerIsTurnedOnInFlight_ThenTheDestinationIsMarkedAsAStation() {
            _markerModel.SetShown(true);

            Assert.IsTrue(_source.TryGetStation(out SonarContact station));
            Assert.AreEqual(_destination, station.Position);
            Assert.AreSame(_configuration.StationKind, station.Kind);
        }

        [Test]
        public void WhenTheMarkerIsOnAndTheShipStandsAtAStation_ThenNoStationIsMarked() {
            _markerModel.SetShown(true);
            _run.Phase = ShipRunPhase.Build;

            Assert.IsFalse(_source.TryGetStation(out _));
        }

        [Test]
        public void WhenTheMarkerIsOnAndTheRunIsNotSyncedYet_ThenNoStationIsMarked() {
            _markerModel.SetShown(true);
            _run.IsAvailable = false;

            Assert.IsFalse(_source.TryGetStation(out _));
        }

        [Test]
        public void WhenTheMarkerIsTurnedOffAgain_ThenTheStationIsGone() {
            _markerModel.SetShown(true);
            _markerModel.SetShown(false);

            Assert.IsFalse(_source.TryGetStation(out _));
        }

        [Test]
        public void WhenTheMarkerIsSetToTheSameState_ThenNoChangeIsTold() {
            _markerModel.OnIsShownChanged += OnIsShownChanged;

            _markerModel.SetShown(true);
            _markerModel.SetShown(true);

            Assert.AreEqual(1, _changeCount);
        }

        private void OnIsShownChanged() =>
            _changeCount++;
    }
}
