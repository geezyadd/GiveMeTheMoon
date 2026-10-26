using Features.ShipModule.Scripts;
using Features.SonarModule.Data;
using Features.SonarModule.Scripts.Core;
using Features.SonarModule.Scripts.Sources;
using NUnit.Framework;
using UnityEngine;

namespace Features.SonarModule.Editor.Tests {
    public sealed class SonarBearingServiceTests {
        private const float RANGE = 1000f;
        private const float TOLERANCE = 0.001f;

        private SonarTestConfiguration _configuration;
        private FakeShipRunModel _run;
        private SonarBearingService _service;

        [SetUp]
        public void SetUp() {
            _configuration = new SonarTestConfiguration();
            _run = new FakeShipRunModel { Phase = ShipRunPhase.Cruise, TransitDestination = new Vector3(0f, 100f, 3000f) };
            _service = new SonarBearingService(new SonarScopeLayout(), new StationSonarContactSource(_run, _configuration.Configuration));
        }

        [TearDown]
        public void TearDown() =>
            _configuration.Dispose();

        [Test]
        public void WhenTheShipHeadsEast_ThenTheHeadingIs90AndNorthIsLeftOnTheScope() {
            SonarScopeBearings bearings = _service.GetBearings(Vector3.zero, 90f, RANGE);

            Assert.AreEqual(90f, bearings.HeadingDegrees, TOLERANCE);
            Assert.AreEqual(-1f, bearings.NorthScopePosition.x, TOLERANCE);
            Assert.AreEqual(0f, bearings.NorthScopePosition.y, TOLERANCE);
        }

        [Test]
        public void WhenTheShipTurnedPastAFullCircle_ThenTheHeadingStaysBelow360() {
            Assert.AreEqual(10f, _service.GetBearings(Vector3.zero, 370f, RANGE).HeadingDegrees, TOLERANCE);
            Assert.AreEqual(350f, _service.GetBearings(Vector3.zero, -10f, RANGE).HeadingDegrees, TOLERANCE);
        }

        [Test]
        public void WhenTheStationLiesBeyondRangeAhead_ThenItIsOnTheTopEdgeWithItsFlatDistance() {
            SonarScopeBearings bearings = _service.GetBearings(new Vector3(0f, 0f, 0f), 0f, RANGE);

            Assert.IsTrue(bearings.HasStation);
            Assert.AreEqual(0f, bearings.StationScopePosition.x, TOLERANCE);
            Assert.AreEqual(1f, bearings.StationScopePosition.y, TOLERANCE);
            Assert.AreEqual(3000f, bearings.StationDistanceMeters, 0.1f);
        }

        [Test]
        public void WhenTheShipStandsAtAStation_ThenTheScopeHasNoStationBearing() {
            _run.Phase = ShipRunPhase.Build;

            Assert.IsFalse(_service.GetBearings(Vector3.zero, 0f, RANGE).HasStation);
        }
    }
}
