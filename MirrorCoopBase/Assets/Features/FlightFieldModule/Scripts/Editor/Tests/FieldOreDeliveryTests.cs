using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Ore;
using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The server side of the harpoon on ore: a rope hit on a piece hooks it (not one off it, not one already on a
    // rope), the hooked piece is pulled to the ship at the pull speed and reaches it once (one OreCollected with its
    // price), the rope breaks when its holder lets go of the weapon, takes another weapon or goes beyond the rope, and
    // a second hook by the same player lets the first piece go. The ore's weapon target id round trip.
    public sealed class FieldOreDeliveryTests {
        private const int SEED = 11;
        private const int LOOP = 0;
        private const string WEAPON_ID = "harpoon";
        private const string OTHER_WEAPON_ID = "pistol";
        private const float PULL_SPEED = 20f;
        private const float MAX_ROPE = 80f;
        private const float PIECE_DISTANCE = 40f;

        private static readonly PlayerKey _hooker = new("client:hooker");
        private static readonly Vector3 _piecePosition = new(0f, 0f, PIECE_DISTANCE);

        private readonly FieldDebugScenario _scenario = new();
        private readonly List<FieldOreCollected> _collected = new();

        private FlightField _field;
        private FieldOreTestRig _rig;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            FlightFieldEditorComposition composition = new FlightFieldEditorCompositionFactory().Create();
            _field = composition.Generator.Generate(_scenario.CreateRequest(composition.RequestFactory, SEED, LOOP));
        }

        [SetUp]
        public void SetUp() {
            _rig = new FieldOreTestRig(_field);
            _rig.Ship.Position = Vector3.zero;
            _rig.Anchors.Set(_hooker, new WeaponHolderAnchor(WEAPON_ID, Vector3.zero));
            _collected.Clear();
            _rig.Collector.OreCollected += _collected.Add;
        }

        [Test]
        public void WhenOreReachesTheShipTwice_ThenTheCrewIsPaidOnce() {
            FieldOreId id = _rig.Spawn(_piecePosition, Vector3.zero);

            Assert.AreEqual(FieldOreCollectResult.Collected, _rig.Collector.ServerCollect(id));
            Assert.AreEqual(FieldOreCollectResult.NotFound, _rig.Collector.ServerCollect(id));

            Assert.AreEqual(1, _collected.Count, "A piece reached the ship more than once.");
            Assert.AreEqual(_rig.FirstKind.Price, _collected[0].Price, "The delivery does not carry the kind's price.");
            Assert.IsFalse(_rig.Query.TryGetOre(id, out _), "A delivered piece is still in the field.");
        }

        [Test]
        public void WhenRopeHooksOre_ThenItIsPulledInAtThePullSpeedAndDeliveredOnce() {
            FieldOreId id = _rig.Spawn(_piecePosition, Vector3.zero);

            Assert.AreEqual(FieldOreHookResult.Hooked, Hook(id, _piecePosition));
            _rig.Clock.Now = 1d;
            Assert.AreEqual(PIECE_DISTANCE - PULL_SPEED, _rig.Describe(id).Center.z, 0.5f, "The piece does not come in at the pull speed.");
            Assert.AreEqual(_hooker, _rig.Describe(id).Hooker, "The piece does not name its hooker.");

            _rig.Clock.Now = 3d;
            _rig.Ropes.ServerUpdateRopes();
            _rig.Ropes.ServerUpdateRopes();

            Assert.AreEqual(1, _collected.Count, "A piece pulled all the way in is not delivered exactly once.");
            Assert.AreEqual(_hooker, _collected[0].Hooker);
        }

        [Test]
        public void WhenRopeHitsOffThePiece_ThenItIsNotHooked() {
            FieldOreId id = _rig.Spawn(_piecePosition, Vector3.zero);

            Assert.AreEqual(FieldOreHookResult.TooFar, Hook(id, _piecePosition + Vector3.right * 10f));
            Assert.IsFalse(_rig.Describe(id).IsHooked);
        }

        [Test]
        public void WhenPieceIsOnARope_ThenASecondRopeIsRefused() {
            FieldOreId id = _rig.Spawn(_piecePosition, Vector3.zero);
            Hook(id, _piecePosition);
            PlayerKey other = new("client:other");

            Assert.AreEqual(FieldOreHookResult.AlreadyHooked, _rig.Hooks.ServerHook(id, other, WEAPON_ID, _piecePosition, new WeaponPull(PULL_SPEED, MAX_ROPE)));
            Assert.AreEqual(_hooker, _rig.Describe(id).Hooker);
        }

        [Test]
        public void WhenHolderLetsGoOfTheHarpoon_ThenTheRopeBreaksAndThePieceDriftsOnFromWhereItIs() {
            FieldOreId id = _rig.Spawn(_piecePosition, Vector3.zero);
            Hook(id, _piecePosition);
            _rig.Clock.Now = 0.5d;
            Vector3 pulledTo = _rig.Describe(id).Center;

            _rig.Anchors.Remove(_hooker);
            _rig.Ropes.ServerUpdateRopes();

            Assert.IsFalse(_rig.Describe(id).IsHooked, "The rope holds with no harpoon in the hand.");
            _rig.Clock.Now = 2d;
            Assert.That(Vector3.Distance(pulledTo, _rig.Describe(id).Center), Is.LessThan(0.01f), "A let-go piece is not left where the rope was.");
        }

        [Test]
        public void WhenHolderTakesAnotherWeapon_ThenTheRopeBreaks() {
            FieldOreId id = _rig.Spawn(_piecePosition, Vector3.zero);
            Hook(id, _piecePosition);

            _rig.Anchors.Set(_hooker, new WeaponHolderAnchor(OTHER_WEAPON_ID, Vector3.zero));
            _rig.Ropes.ServerUpdateRopes();

            Assert.IsFalse(_rig.Describe(id).IsHooked, "The rope holds after the weapon changed.");
        }

        [Test]
        public void WhenHolderGoesBeyondTheRope_ThenTheRopeBreaks() {
            FieldOreId id = _rig.Spawn(_piecePosition, Vector3.zero);
            Hook(id, _piecePosition);

            _rig.Anchors.Set(_hooker, new WeaponHolderAnchor(WEAPON_ID, _piecePosition + Vector3.back * (MAX_ROPE + 1f)));
            _rig.Ropes.ServerUpdateRopes();

            Assert.IsFalse(_rig.Describe(id).IsHooked, "The rope holds beyond its length.");
        }

        [Test]
        public void WhenPlayerHooksASecondPiece_ThenTheFirstIsLetGo() {
            FieldOreId first = _rig.Spawn(_piecePosition, Vector3.zero);
            Vector3 secondPosition = _piecePosition + Vector3.right * 10f;
            FieldOreId second = _rig.Spawn(secondPosition, Vector3.zero);
            Hook(first, _piecePosition);

            Assert.AreEqual(FieldOreHookResult.Hooked, Hook(second, secondPosition));

            Assert.IsFalse(_rig.Describe(first).IsHooked, "The first piece stays on the rope.");
            Assert.IsTrue(_rig.Describe(second).IsHooked);
        }

        [Test]
        public void WhenOreIdIsPackedIntoAWeaponTargetId_ThenItComesBackAndARockIdDoesNot() {
            FieldOreWeaponIds ids = new();
            FieldOreId id = new(1234);

            Assert.IsTrue(ids.TryGetOreId(ids.GetTargetId(id), out FieldOreId back));
            Assert.AreEqual(id, back);
            Assert.IsFalse(ids.TryGetOreId(0xF100000000000001UL, out _), "A rock's target id reads as ore.");
        }

        private FieldOreHookResult Hook(FieldOreId id, Vector3 hitPoint) =>
            _rig.Hooks.ServerHook(id, _hooker, WEAPON_ID, hitPoint, new WeaponPull(PULL_SPEED, MAX_ROPE));
    }
}
