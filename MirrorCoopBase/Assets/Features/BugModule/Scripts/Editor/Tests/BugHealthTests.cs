using Features.BugModule.Scripts.Brain;
using Features.BugModule.Scripts.Health;
using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Features.BugModule.Scripts.Editor.Tests {
    // A bug is a weapon target with small health: hits take it down, the hit that leaves none kills it once.
    public sealed class BugHealthTests {
        private const int BUG_ID = 42;
        private const float MAX_HEALTH = 10f;
        private const float MINIGUN_DAMAGE = 1f;
        private const float PISTOL_DAMAGE = 12f;
        private const string PISTOL = "pistol";
        private const ulong ROCK_TARGET_ID = 0xF100000000000001UL;
        private const int MINIGUN_HITS_LEAVING_ONE = 9;
        private const int MINIGUN_HITS_TO_KILL = 10;
        private const float HEALTH_TOLERANCE = 0.0001f;

        private BugServerModel _model;
        private RecordingBugRemovalService _removal;
        private BugWeaponIds _ids;
        private RecordingWeaponTargetRegistry _registry;
        private BugWeaponTargetSystem _targets;

        [SetUp]
        public void SetUp() {
            _model = new BugServerModel();
            _model.Add(new BugAgent(BUG_ID, 0f, MAX_HEALTH));
            _removal = new RecordingBugRemovalService(_model);
            _ids = new BugWeaponIds();
            _registry = new RecordingWeaponTargetRegistry();
            _targets = new BugWeaponTargetSystem(_registry, _ids, new BugWeaponTarget(new BugDamageService(_model, _removal)));
        }

        [Test]
        public void WhenTheSessionStartsAndEnds_ThenTheBugsJoinAndLeaveTheWeaponHitPathAsOneSource() {
            _targets.Initialize();
            CollectionAssert.AreEqual(new[] { _targets }, _registry.Sources);

            _targets.Dispose();
            Assert.IsEmpty(_registry.Sources);
        }

        [Test]
        public void WhenABugIdBecomesAWeaponTargetId_ThenItComesBackTheSameAndARockIdIsNoBug() {
            Assert.IsTrue(_ids.TryGetBugId(_ids.GetTargetId(BUG_ID), out int back));
            Assert.AreEqual(BUG_ID, back);
            Assert.IsFalse(_ids.TryGetBugId(ROCK_TARGET_ID, out _));
            Assert.IsFalse(_targets.TryGetTarget(ROCK_TARGET_ID, out _));
        }

        [Test]
        public void WhenMinigunHitsLeaveHealth_ThenTheBugStaysAlive() {
            IWeaponTarget target = AimAtBug();
            for (int i = 0; i < MINIGUN_HITS_LEAVING_ONE; i++)
                target.ServerReceiveHit(CreateHit(MINIGUN_DAMAGE));

            Assert.IsTrue(target.IsAlive);
            Assert.IsTrue(_model.TryGet(BUG_ID, out BugAgent bug));
            Assert.AreEqual(MAX_HEALTH - MINIGUN_HITS_LEAVING_ONE * MINIGUN_DAMAGE, bug.Health, HEALTH_TOLERANCE);
            Assert.IsEmpty(_removal.Killed);
        }

        [Test]
        public void WhenTheHealthReachesZero_ThenTheBugIsKilledOnceAndIsNoLongerAlive() {
            IWeaponTarget target = AimAtBug();
            for (int i = 0; i < MINIGUN_HITS_TO_KILL; i++)
                target.ServerReceiveHit(CreateHit(MINIGUN_DAMAGE));

            CollectionAssert.AreEqual(new[] { BUG_ID }, _removal.Killed);
            Assert.IsFalse(AimAtBug().IsAlive);
            AimAtBug().ServerReceiveHit(CreateHit(MINIGUN_DAMAGE));
            Assert.AreEqual(1, _removal.Killed.Count, "A dead bug was killed again.");
        }

        [Test]
        public void WhenOnePistolShotHits_ThenTheBugDies() {
            AimAtBug().ServerReceiveHit(CreateHit(PISTOL_DAMAGE));

            CollectionAssert.AreEqual(new[] { BUG_ID }, _removal.Killed);
            Assert.AreEqual(0, _model.Count);
        }

        private IWeaponTarget AimAtBug() {
            Assert.IsTrue(_targets.TryGetTarget(_ids.GetTargetId(BUG_ID), out IWeaponTarget target));
            return target;
        }

        private WeaponHit CreateHit(float damage) =>
            new(_ids.GetTargetId(BUG_ID), Vector3.zero, Vector3.up, damage, default(PlayerKey), PISTOL);
    }
}
