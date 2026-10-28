using System.Collections.Generic;
using Features.BugModule.Scripts.Brain;
using Features.FieldAgentModule.Scripts.Cargo;
using Features.ShipDamageModule.Scripts.Damage;
using NUnit.Framework;
using UnityEngine;

namespace Features.BugModule.Scripts.Editor.Tests {
    public sealed class BugTargetRuleTests {
        private const string LEFT_ENGINE = "engine-left";
        private const string RIGHT_ENGINE = "engine-right";
        private const string HULL_POINT = "breach-01";

        private static readonly Vector3 _leftSide = new(-10f, 2f, 0f);
        private static readonly Vector3 _rightSide = new(10f, 2f, 0f);

        private readonly BugTargetRule _rule = new();

        [Test]
        public void WhenACargoIsOnBoard_ThenTheBugGoesForTheCargoInsteadOfGnawing() {
            FakeCargoTarget cargo = new(new Vector3(0f, 0.5f, -3f));

            BugTarget target = _rule.Choose(new List<IFieldAgentCargoTarget> { cargo }, CreateSpots(), _leftSide);

            Assert.AreEqual(BugTargetKind.Cargo, target.Kind);
            Assert.AreSame(cargo, target.Cargo);
        }

        [Test]
        public void WhenSeveralCargosAreOnBoard_AndOneIsTaken_ThenTheBugGoesForTheNearestFreeOne() {
            FakeCargoTarget takenNear = new(new Vector3(-9f, 0f, 0f), true);
            FakeCargoTarget freeFar = new(new Vector3(8f, 0f, 0f));
            FakeCargoTarget freeNear = new(new Vector3(-4f, 0f, 0f));

            BugTarget target = _rule.Choose(new List<IFieldAgentCargoTarget> { takenNear, freeFar, freeNear }, CreateSpots(), _leftSide);

            Assert.AreSame(freeNear, target.Cargo);
        }

        [Test]
        public void WhenNoCargoIsOnBoard_ThenTheBugGnawsTheDamageablePlaceNearestToItsSide() {
            BugTarget left = _rule.Choose(new List<IFieldAgentCargoTarget>(), CreateSpots(), _leftSide);
            BugTarget right = _rule.Choose(new List<IFieldAgentCargoTarget>(), CreateSpots(), _rightSide);

            Assert.AreEqual(BugTargetKind.Gnaw, left.Kind);
            Assert.AreEqual(new Vector3(-3f, 0f, 1f), left.GnawPoint);
            Assert.AreEqual(new Vector3(3f, 0f, 1f), right.GnawPoint);
        }

        [Test]
        public void WhenTheOnlyCargoIsTaken_ThenTheBugGnawsTheShip() {
            BugTarget target = _rule.Choose(new List<IFieldAgentCargoTarget> { new FakeCargoTarget(Vector3.zero, true) }, CreateSpots(), _leftSide);

            Assert.AreEqual(BugTargetKind.Gnaw, target.Kind);
        }

        [Test]
        public void WhenTheNearestPlaceCannotTakeDamage_ThenTheBugGnawsTheNearestThatCan() {
            List<ShipDamageCandidate> spots = new() {
                new ShipDamageCandidate(LEFT_ENGINE, new Vector3(-3f, 0f, 1f), false, 1f),
                new ShipDamageCandidate(HULL_POINT, new Vector3(0f, 0f, -5f), true, 1f),
            };

            BugTarget target = _rule.Choose(new List<IFieldAgentCargoTarget>(), spots, _leftSide);

            Assert.AreEqual(new Vector3(0f, 0f, -5f), target.GnawPoint);
        }

        [Test]
        public void WhenNoPlaceCanTakeDamage_ThenTheBugStillGnawsTheNearestPlace() {
            List<ShipDamageCandidate> spots = new() {
                new ShipDamageCandidate(LEFT_ENGINE, new Vector3(-3f, 0f, 1f), false, 1f),
                new ShipDamageCandidate(RIGHT_ENGINE, new Vector3(3f, 0f, 1f), false, 1f),
            };

            BugTarget target = _rule.Choose(new List<IFieldAgentCargoTarget>(), spots, _rightSide);

            Assert.AreEqual(new Vector3(3f, 0f, 1f), target.GnawPoint);
        }

        [Test]
        public void WhenTheShipHasNoPlaces_ThenTheBugGnawsTheMiddleOfTheDeck() {
            BugTarget target = _rule.Choose(new List<IFieldAgentCargoTarget>(), new List<ShipDamageCandidate>(), _rightSide);

            Assert.AreEqual(BugTargetKind.Gnaw, target.Kind);
            Assert.AreEqual(Vector3.zero, target.GnawPoint);
        }

        private static List<ShipDamageCandidate> CreateSpots() =>
            new() {
                new ShipDamageCandidate(LEFT_ENGINE, new Vector3(-3f, 0f, 1f), true, 1f),
                new ShipDamageCandidate(RIGHT_ENGINE, new Vector3(3f, 0f, 1f), true, 1f),
                new ShipDamageCandidate(HULL_POINT, new Vector3(0f, 0f, -5f), true, 1f),
            };
    }
}
