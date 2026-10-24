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
