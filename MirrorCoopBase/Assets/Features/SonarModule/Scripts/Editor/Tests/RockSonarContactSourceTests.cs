using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.SonarModule.Contracts;
using Features.SonarModule.Scripts.Sources;
using NUnit.Framework;
using UnityEngine;

namespace Features.SonarModule.Editor.Tests {
    public sealed class RockSonarContactSourceTests {
        private const float RANGE = 500f;
        private const float FLIGHT_HEIGHT = 100f;
        private const float LARGE_DIAMETER = 80f;
        private const float SMALL_DIAMETER = 12f;
        private const float TOLERANCE = 0.01f;
        private const string STRUCTURE_ID = "rock";

        private SonarTestConfiguration _configuration;
        private FakeFieldRockQuery _rocks;
        private RockSonarContactSource _source;
        private readonly List<SonarContact> _contacts = new();

        [SetUp]
        public void SetUp() {
            _configuration = new SonarTestConfiguration();
            _rocks = new FakeFieldRockQuery();
            _source = new RockSonarContactSource(_rocks, new RockSonarContactIds(), _configuration.Configuration);
            _contacts.Clear();
        }

        [TearDown]
        public void TearDown() =>
            _configuration.Dispose();

        [Test]
        public void WhenALargeRockIsInRange_ThenItIsOneDangerContact() {
            _rocks.Add(LargeRock(0, new Vector3(0f, FLIGHT_HEIGHT, 300f)));

            _source.CollectContacts(new Vector3(0f, FLIGHT_HEIGHT, 0f), RANGE, _contacts);

            Assert.AreEqual(1, _contacts.Count);
            Assert.AreSame(_configuration.LargeRockKind, _contacts[0].Kind);
        }

        [Test]
        public void WhenALargeRockIsOutOfRange_ThenTheSonarDoesNotSeeIt() {
            _rocks.Add(LargeRock(0, new Vector3(0f, FLIGHT_HEIGHT, RANGE + LARGE_DIAMETER)));

            _source.CollectContacts(new Vector3(0f, FLIGHT_HEIGHT, 0f), RANGE, _contacts);

            Assert.AreEqual(0, _contacts.Count);
        }

        [Test]
        public void WhenTooFewSmallRocksShareACell_ThenThereIsNoDebrisContact() {
            AddSmallRocks(SonarTestConfiguration.DEBRIS_MIN_ROCKS - 1);

            _source.CollectContacts(new Vector3(0f, FLIGHT_HEIGHT, 0f), RANGE, _contacts);

            Assert.AreEqual(0, _contacts.Count);
        }

        [Test]
        public void WhenEnoughSmallRocksShareACell_ThenTheyAreOneDebrisContactAtTheirMiddle() {
            List<Vector3> positions = AddSmallRocks(SonarTestConfiguration.DEBRIS_MIN_ROCKS);

            _source.CollectContacts(new Vector3(0f, FLIGHT_HEIGHT, 0f), RANGE, _contacts);

            Assert.AreEqual(1, _contacts.Count);
            Assert.AreSame(_configuration.DebrisKind, _contacts[0].Kind);
            Assert.Less(Vector3.Distance(Middle(positions), _contacts[0].Position), TOLERANCE);
        }

        [Test]
        public void WhenADebrisContactIsLookedUpById_ThenItIsFoundAtFlightHeight() {
            AddSmallRocks(SonarTestConfiguration.DEBRIS_MIN_ROCKS);
            _source.CollectContacts(new Vector3(0f, FLIGHT_HEIGHT, 0f), RANGE, _contacts);

            Assert.IsTrue(_source.TryGetContact(_contacts[0].Id, out SonarContact debris));
            Assert.Less(Vector3.Distance(_contacts[0].Position, debris.Position), TOLERANCE);
        }

        [Test]
        public void WhenALargeRockBreaks_ThenItsContactIsGone() {
            FieldRockDescription rock = LargeRock(0, new Vector3(0f, FLIGHT_HEIGHT, 200f));
            _rocks.Add(rock);
            _source.CollectContacts(new Vector3(0f, FLIGHT_HEIGHT, 0f), RANGE, _contacts);
            string contactId = _contacts[0].Id;
            Assert.IsTrue(_source.TryGetContact(contactId, out _));

            _rocks.Break(rock.Id);

            Assert.IsFalse(_source.TryGetContact(contactId, out _));
            _contacts.Clear();
            _source.CollectContacts(new Vector3(0f, FLIGHT_HEIGHT, 0f), RANGE, _contacts);
            Assert.AreEqual(0, _contacts.Count);
        }

        [Test]
        public void WhenADebrisRockIdComesBack_ThenItNamesTheSameRock() {
            RockSonarContactIds ids = new RockSonarContactIds();
            FieldRockId rockId = FieldRockId.CreateDebris(new Vector2Int(-4, 17), 3);

            Assert.IsTrue(ids.TryParseRockId(ids.CreateRockId(rockId), out FieldRockId parsed));
            Assert.AreEqual(rockId, parsed);
            Assert.IsFalse(ids.TryParseRockId("station", out _));
        }

        private List<Vector3> AddSmallRocks(int count) {
            List<Vector3> positions = new List<Vector3>();
            for (int i = 0; i < count; i++) {
                Vector3 position = new Vector3(110f + i * 15f, FLIGHT_HEIGHT, 120f + i * 10f);
                positions.Add(position);
                _rocks.Add(new FieldRockDescription(FieldRockId.CreateDebris(new Vector2Int(1, 1), i), STRUCTURE_ID, position, SMALL_DIAMETER, FieldRockSize.Small, true));
            }

            return positions;
        }

        private static FieldRockDescription LargeRock(int placement, Vector3 position) =>
            new(FieldRockId.CreateCluster(placement, 0), STRUCTURE_ID, position, LARGE_DIAMETER, FieldRockSize.Large, true);

        private static Vector3 Middle(List<Vector3> positions) {
            Vector3 sum = Vector3.zero;
            foreach (Vector3 position in positions)
                sum += position;

            return sum / positions.Count;
        }
    }
}
