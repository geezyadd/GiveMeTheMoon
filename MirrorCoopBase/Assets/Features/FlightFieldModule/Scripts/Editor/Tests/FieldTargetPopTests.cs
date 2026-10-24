using System.Collections.Generic;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.FlightFieldModule.Scripts.Server;
using Features.FlightFieldModule.Scripts.Targets;
using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The server side of the target balloons over a generated field: the query finds a balloon from the field data and
    // leaves a popped one out; a balloon pops once whoever asks (TargetPopped once, in the popped list); weapon hits add
    // up to the kind's hit points and pop it as a Weapon pop by the shooter; a hit on a popped balloon or off the
    // balloon gives nothing; the popped list and the damage go with the field; the weapon target id round trip.
    public sealed class FieldTargetPopTests {
        private const int SEED = 11;
        private const int LOOP = 3;
        private const string WEAPON_ID = "minigun";
        private const float DAMAGE_PER_HIT = 1f;
        private const float OFF_TARGET_METERS = 40f;

        private static readonly PlayerKey _shooter = new("client:shooter");
        private static readonly PlayerKey _noShooter = new(string.Empty);

        private readonly FieldDebugScenario _scenario = new();
        private readonly List<FieldTargetPopped> _popped = new();

        private FlightFieldEditorComposition _composition;
        private FieldTargetCatalog _catalog;
        private FieldTargetConfiguration _configuration;
        private FlightField _field;
        private FlightFieldServerModel _serverModel;
        private RecordingPoppedTargets _poppedTargets;
        private FieldTargetQuery _query;
        private FieldTargetPopper _popper;
        private FieldTargetDamageModel _damageModel;
        private FieldTargetDamageService _damageService;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _composition = new FlightFieldEditorCompositionFactory().Create();
            _catalog = AddressableConfigurationLoader.Load<FieldTargetCatalog>(Address.Configurations.FieldTargetCatalog_Default);
            _configuration = AddressableConfigurationLoader.Load<FieldTargetConfiguration>(Address.Configurations.FieldTargetConfiguration_Default);
            _field = _composition.Generator.Generate(_scenario.CreateRequest(_composition.RequestFactory, SEED, LOOP));
        }

        [SetUp]
        public void SetUp() {
            Assert.Greater(_field.Targets.Count, 1, "The test field has fewer than two balloons.");
            _serverModel = new FlightFieldServerModel();
            _serverModel.SetField(_field);
            _poppedTargets = new RecordingPoppedTargets();
            _query = new FieldTargetQuery(_serverModel, _catalog, _poppedTargets);
            _popper = new FieldTargetPopper(_serverModel, _query, _poppedTargets, _poppedTargets);
            _damageModel = new FieldTargetDamageModel();
            _damageService = new FieldTargetDamageService(_query, _popper, _catalog, _damageModel, _configuration);
            _popped.Clear();
            _popper.TargetPopped += _popped.Add;
        }

        [Test]
        public void WhenABalloonIsQueried_ThenItIsFoundUntilItPops() {
            FieldTargetDescription target = Describe(0);
            List<FieldTargetDescription> found = new();

            _query.FindInSphere(target.Center, 1f, found);
            Assert.IsTrue(found.Exists(each => each.Id.Equals(target.Id)), "A sphere at the balloon's middle does not find it.");
            _query.FindInBox(target.Center + Vector3.right * target.Radius, Vector3.one, Quaternion.Euler(0f, 30f, 0f), found);
            Assert.IsTrue(found.Exists(each => each.Id.Equals(target.Id)), "A box touching the balloon's sphere does not find it.");
            Assert.AreEqual(_catalog.GetRequired(_field.Targets[0].KindId).Reward, target.Reward, "The balloon does not carry its kind's reward.");

            _popper.ServerPop(target.Id, FieldTargetPopCause.ShipCollision, _noShooter);
            _query.FindInSphere(target.Center, 1f, found);
            Assert.IsFalse(found.Exists(each => each.Id.Equals(target.Id)), "A popped balloon is still found.");
            Assert.IsFalse(_query.TryGetTarget(target.Id, out _), "A popped balloon is still a live target.");
        }

        [Test]
        public void WhenABalloonPopsTwice_ThenItPopsOnceWithOneEvent() {
            FieldTargetDescription target = Describe(0);

            Assert.AreEqual(FieldTargetPopResult.Popped, _popper.ServerPop(target.Id, FieldTargetPopCause.ShipCollision, _noShooter));
            Assert.AreEqual(FieldTargetPopResult.AlreadyPopped, _popper.ServerPop(target.Id, FieldTargetPopCause.ShipCollision, _noShooter));
            Assert.AreEqual(FieldTargetPopResult.AlreadyPopped, _popper.ServerPop(target.Id, FieldTargetPopCause.Weapon, _shooter, WEAPON_ID));

            Assert.AreEqual(1, _popped.Count, "TargetPopped must fire once per balloon.");
            Assert.AreEqual(FieldTargetPopCause.ShipCollision, _popped[0].Cause);
            Assert.AreEqual(target.Reward, _popped[0].Reward);
            Assert.AreEqual(target.KindId, _popped[0].KindId);
            Assert.AreEqual(LOOP, _popped[0].LoopIndex);
            Assert.IsTrue(_poppedTargets.IsPopped(target.Id), "The popped balloon is not in the popped list.");
            Assert.AreEqual(FieldTargetPopResult.NotFound, _popper.ServerPop(new FieldTargetId(_field.Targets.Count), FieldTargetPopCause.ShipCollision, _noShooter));
        }

        [Test]
        public void WhenTheMinigunHitsABalloonUpToItsHitPoints_ThenItPopsAsAWeaponPopByTheShooter() {
            FieldTargetDescription target = Describe(0);
            int hitsToPop = Mathf.CeilToInt(_catalog.GetRequired(target.KindId).HitPoints / DAMAGE_PER_HIT);

            for (int hit = 1; hit < hitsToPop; hit++)
                Assert.AreEqual(FieldTargetDamageResult.Damaged, _damageService.ServerApplyHit(target.Id, CreateHit(target.Center)), $"Hit {hit} popped the balloon too early.");

            Assert.AreEqual(FieldTargetDamageResult.Popped, _damageService.ServerApplyHit(target.Id, CreateHit(target.Center)));
            Assert.AreEqual(1, _popped.Count);
            Assert.AreEqual(FieldTargetPopCause.Weapon, _popped[0].Cause);
            Assert.AreEqual(_shooter, _popped[0].Shooter);
            Assert.AreEqual(WEAPON_ID, _popped[0].WeaponId);
        }

        [Test]
        public void WhenAPoppedBalloonIsHitAgain_ThenTheHitGivesNothing() {
            FieldTargetDescription target = Describe(0);
            _popper.ServerPop(target.Id, FieldTargetPopCause.ShipCollision, _noShooter);

            Assert.AreEqual(FieldTargetDamageResult.NotFound, _damageService.ServerApplyHit(target.Id, CreateHit(target.Center)));
            Assert.AreEqual(1, _popped.Count, "A hit on a popped balloon fired TargetPopped again.");
        }

        [Test]
        public void WhenAHitLandsOffTheBalloon_ThenItIsRefused() {
            FieldTargetDescription target = Describe(1);

            Assert.AreEqual(FieldTargetDamageResult.TooFar, _damageService.ServerApplyHit(target.Id, CreateHit(target.Center + Vector3.up * OFF_TARGET_METERS)));
            Assert.AreEqual(0f, _damageModel.GetDamageTaken(target.Id));
            Assert.IsEmpty(_popped);
        }

        [Test]
        public void WhenTheFieldIsReset_ThenThePoppedListAndTheDamageAreForgotten() {
            FieldTargetDescription popped = Describe(0);
            FieldTargetDescription damaged = Describe(1);
            _popper.ServerPop(popped.Id, FieldTargetPopCause.ShipCollision, _noShooter);
            _damageModel.SetDamageTaken(damaged.Id, DAMAGE_PER_HIT);

            _popper.ServerReset();
            _damageService.ServerReset();

            Assert.AreEqual(0, _poppedTargets.Count, "The popped list survived the field reset.");
            Assert.AreEqual(0f, _damageModel.GetDamageTaken(damaged.Id), "The damage survived the field reset.");
        }

        [Test]
        public void WhenABalloonIdIsPackedIntoAWeaponTargetId_ThenItUnpacksToTheSameBalloonAndNoRockIdDoes() {
            FieldTargetWeaponIds ids = new();
            FieldTargetId id = new(_field.Targets.Count - 1);

            Assert.IsTrue(ids.TryGetBalloonId(ids.GetTargetId(id), out FieldTargetId unpacked));
            Assert.AreEqual(id, unpacked);
            ulong rockTargetId = new FieldRockTargetIds().GetTargetId(FieldRockId.CreateCluster(id.Index, 0));
            Assert.IsFalse(ids.TryGetBalloonId(rockTargetId, out _), "A rock's weapon target id reads as a balloon.");
        }

        private FieldTargetDescription Describe(int index) {
            Assert.IsTrue(_query.TryGetTarget(new FieldTargetId(index), out FieldTargetDescription target), $"Balloon {index} is not found.");
            return target;
        }

        private static WeaponHit CreateHit(Vector3 point) =>
            new(0UL, point, Vector3.up, DAMAGE_PER_HIT, _shooter, WEAPON_ID);
    }
}
