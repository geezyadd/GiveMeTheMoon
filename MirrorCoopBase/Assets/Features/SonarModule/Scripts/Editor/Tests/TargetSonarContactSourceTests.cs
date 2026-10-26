using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.SonarModule.Contracts;
using Features.SonarModule.Scripts.Sources;
using NUnit.Framework;
using UnityEngine;

namespace Features.SonarModule.Editor.Tests {
    public sealed class TargetSonarContactSourceTests {
        private const float RANGE = 500f;
        private const float BALLOON_RADIUS = 4f;
        private const int REWARD = 50;
        private const string KIND_ID = "balloon";

        private SonarTestConfiguration _configuration;
        private FakeFieldTargetQuery _targets;
        private TargetSonarContactSource _source;
        private readonly List<SonarContact> _contacts = new();

        [SetUp]
        public void SetUp() {
            _configuration = new SonarTestConfiguration();
            _targets = new FakeFieldTargetQuery();
            _source = new TargetSonarContactSource(_targets, _configuration.Configuration);
            _contacts.Clear();
        }

        [TearDown]
        public void TearDown() =>
            _configuration.Dispose();

        [Test]
        public void WhenABalloonIsInRange_ThenItIsATargetContactFoundAgainById() {
            _targets.Add(Balloon(3, new Vector3(0f, 100f, 200f)));

            _source.CollectContacts(new Vector3(0f, 100f, 0f), RANGE, _contacts);

            Assert.AreEqual(1, _contacts.Count);
            Assert.AreSame(_configuration.TargetKind, _contacts[0].Kind);
            Assert.IsTrue(_source.TryGetContact(_contacts[0].Id, out SonarContact again));
            Assert.AreEqual(_contacts[0].Position, again.Position);
        }

        [Test]
        public void WhenABalloonGivesAHighReward_ThenItIsTheKindOfItsRewardTier() {
            _targets.Add(new FieldTargetDescription(new FieldTargetId(1), KIND_ID, new Vector3(0f, 100f, 200f), BALLOON_RADIUS, SonarTestConfiguration.GOLDEN_TARGET_MIN_REWARD));
            _targets.Add(new FieldTargetDescription(new FieldTargetId(2), KIND_ID, new Vector3(0f, 100f, 300f), BALLOON_RADIUS, SonarTestConfiguration.GOLDEN_TARGET_MIN_REWARD - 1));

            _source.CollectContacts(new Vector3(0f, 100f, 0f), RANGE, _contacts);

            Assert.AreEqual(2, _contacts.Count);
            Assert.AreSame(_configuration.GoldenTargetKind, _contacts[0].Kind);
            Assert.AreSame(_configuration.TargetKind, _contacts[1].Kind);
            Assert.IsTrue(_source.TryGetContact(_contacts[0].Id, out SonarContact golden));
            Assert.AreSame(_configuration.GoldenTargetKind, golden.Kind);
        }

        [Test]
        public void WhenABalloonIsOutOfRange_ThenTheSonarDoesNotSeeIt() {
            _targets.Add(Balloon(0, new Vector3(0f, 100f, RANGE * 2f)));

            _source.CollectContacts(new Vector3(0f, 100f, 0f), RANGE, _contacts);

            Assert.AreEqual(0, _contacts.Count);
        }

        [Test]
        public void WhenABalloonPops_ThenItsContactIsGone() {
            _targets.Add(Balloon(7, new Vector3(0f, 100f, 100f)));
            _source.CollectContacts(new Vector3(0f, 100f, 0f), RANGE, _contacts);
            string contactId = _contacts[0].Id;

            _targets.Pop(new FieldTargetId(7));

            Assert.IsFalse(_source.TryGetContact(contactId, out _));
            Assert.IsFalse(_source.TryGetContact("rock:c:7:0", out _));
        }

        private static FieldTargetDescription Balloon(int index, Vector3 position) =>
            new(new FieldTargetId(index), KIND_ID, position, BALLOON_RADIUS, REWARD);
    }
}
