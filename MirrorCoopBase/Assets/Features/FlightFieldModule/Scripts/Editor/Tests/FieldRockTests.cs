using System.Collections.Generic;
using System.Linq;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.NetworkModelModule.Scripts;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The breakable rock base: every rock has the same id and pose however and wherever the field is read (two
    // independent builds, a world shift, a peer's debris from the published field), the server's query finds exactly
    // the rocks touching a volume and leaves broken ones out, a rock breaks once, and the broken list goes with the field.
    public sealed class FieldRockTests {
        private const string GIANTS_PROFILE = "obstacles_giants";
        private const int SEED = 7;
        private const int LOOP = 2;
        private const float PROBE_RADIUS = 120f;
        private const int PROBE_STEP_CELLS = 4;
        private const float POSITION_TOLERANCE = 0.001f;
        private const int FIELD_NUMBER = 1;
        private const int MAX_DEBRIS_CELLS_COMPARED = 400;

        private static readonly Vector3 _probeBoxHalfExtents = new(60f, 30f, 90f);
        private static readonly Vector3 _worldShift = new(-812.5f, 0f, 431.25f);

        private readonly FieldDebugScenario _scenario = new();
        private readonly Dictionary<string, FlightField> _fieldsByProfile = new();

        private FlightFieldEditorComposition _composition;
        private FieldRockDurabilityConfiguration _durability;

        [OneTimeSetUp]
        public void OneTimeSetUp() {
            _composition = new FlightFieldEditorCompositionFactory().Create();
            _durability = AddressableConfigurationLoader.Load<FieldRockDurabilityConfiguration>(Address.Configurations.FieldRockDurabilityConfiguration_Default);
        }

        [TestCase(GIANTS_PROFILE)]
        public void WhenTwoIndependentBuildsReadTheSameField_ThenEveryRockHasTheSameIdAndPose(string profileId) {
            FlightField field = GetField(profileId);
            List<Vector3> probes = CreateProbes(field.Frame);

            Dictionary<FieldRockId, FieldRockDescription> first = FindAll(new FieldRockTestRig(_composition, _durability, field), probes, Vector3.zero);
            probes.Reverse();
            Dictionary<FieldRockId, FieldRockDescription> second = FindAll(new FieldRockTestRig(_composition, _durability, field), probes, Vector3.zero);

            Assert.IsNotEmpty(first, $"{profileId}: no rock found along the field.");
            AssertSameRocks(first, second, Vector3.zero, profileId);
        }

        [Test]
        public void WhenTheWorldShifts_ThenEveryRockKeepsItsIdAndMovesWithTheShift() {
            FlightField field = GetField(GIANTS_PROFILE);
            List<Vector3> probes = CreateProbes(field.Frame);
            FieldRockTestRig shifted = new(_composition, _durability, field);
            shifted.ServerModel.ShiftFrame(_worldShift);

            Dictionary<FieldRockId, FieldRockDescription> before = FindAll(new FieldRockTestRig(_composition, _durability, field), probes, Vector3.zero);
            Dictionary<FieldRockId, FieldRockDescription> after = FindAll(shifted, probes, _worldShift);

            AssertSameRocks(before, after, _worldShift, GIANTS_PROFILE);
        }

        [TestCase(GIANTS_PROFILE)]
        public void WhenAPeerLaysOutDebrisFromThePublishedField_ThenItMatchesTheServer(string profileId) {
            FlightField field = GetField(profileId);
            RecordingFieldPublishTarget published = Publish(field);
            LiveFieldDebrisCells peerCells = new();
            peerCells.Read(published.DebrisRuns, published.Placements, _composition.Catalog, _composition.FootprintReader, _composition.ProfileLookup.GetRequired(field.ProfileId));
            MatrixFieldDebrisCells serverCells = new(field.Cells, _composition.Catalog, _composition.ProfileLookup.GetRequired(field.ProfileId));
            FieldDebrisWorkspace peerWorkspace = new(new FieldRandomFactory().Create(0));
            FieldDebrisWorkspace serverWorkspace = new(new FieldRandomFactory().Create(0));
            List<FieldDebrisSpot> peerSpots = new();
            List<FieldDebrisSpot> serverSpots = new();

            int compared = 0;
            foreach (Vector2Int cell in DebrisCells(field).Take(MAX_DEBRIS_CELLS_COMPARED)) {
                _composition.DebrisLayout.Fill(field.Seed, cell, field.Frame.CellSize, peerCells, peerWorkspace, peerSpots);
                _composition.DebrisLayout.Fill(field.Seed, cell, field.Frame.CellSize, serverCells, serverWorkspace, serverSpots);
                Assert.AreEqual(serverSpots.Count, peerSpots.Count, $"{profileId} cell {cell}: a peer lays out another number of rocks.");
                for (int i = 0; i < serverSpots.Count; i++)
                    Assert.Less((serverSpots[i].Offset - peerSpots[i].Offset).magnitude, POSITION_TOLERANCE, $"{profileId} cell {cell} rock {i} stands elsewhere on a peer.");

                compared++;
            }

            Assert.Greater(compared, 0, $"{profileId}: the field has no debris to compare.");
        }

        [TestCase(GIANTS_PROFILE)]
        public void WhenAVolumeIsQueried_ThenItFindsExactlyTheRocksTouchingIt(string profileId) {
            FlightField field = GetField(profileId);
            FieldRockTestRig rig = new(_composition, _durability, field);
            List<FieldRockDescription> everyRock = ReadEveryRock(rig, field);
            List<FieldRockDescription> found = new();

            foreach (Vector3 probe in CreateProbes(field.Frame)) {
                Quaternion turn = Quaternion.Euler(0f, probe.x % 90f, 0f);
                rig.Query.FindInSphere(probe, PROBE_RADIUS, found);
                AssertSameIds(everyRock.Where(rock => FieldRockVolume.CreateSphere(probe, PROBE_RADIUS).Intersects(rock.Center, rock.Radius)), found, $"{profileId} sphere at {probe}");
                rig.Query.FindInBox(probe, _probeBoxHalfExtents, turn, found);
                AssertSameIds(everyRock.Where(rock => FieldRockVolume.CreateBox(probe, _probeBoxHalfExtents, turn).Intersects(rock.Center, rock.Radius)), found, $"{profileId} box at {probe}");
            }
        }

        [TestCase(GIANTS_PROFILE)]
        public void WhenAPeerQueriesThePublishedField_ThenItFindsTheSameRocksAsTheServer(string profileId) {
            FlightField field = GetField(profileId);
            FieldRockTestRig rig = new(_composition, _durability, field);
            PublishedFieldRockReader publishedReader = new(_composition.Catalog, _composition.FootprintReader, _composition.ProfileLookup);
            FieldRockQuery peerQuery = new(new RecordedFieldRockFieldSource(Publish(field), publishedReader), _composition.Catalog, _composition.Configuration, rig.Reader,
                rig.BrokenRocks, new FieldRandomFactory());
            List<FieldRockDescription> serverFound = new();
            List<FieldRockDescription> peerFound = new();

            int compared = 0;
            foreach (Vector3 probe in CreateProbes(field.Frame)) {
                rig.Query.FindInSphere(probe, PROBE_RADIUS, serverFound);
                peerQuery.FindInSphere(probe, PROBE_RADIUS, peerFound);
                AssertSameRocks(serverFound.ToDictionary(rock => rock.Id), peerFound.ToDictionary(rock => rock.Id), Vector3.zero, $"{profileId} peer sphere at {probe}");
                compared += serverFound.Count;
            }

            Assert.Greater(compared, 0, $"{profileId}: no rock to compare.");
            FieldRockDescription anyRock = ReadEveryRock(rig, field).First();
            Assert.IsTrue(peerQuery.TryGetRock(anyRock.Id, out FieldRockDescription peerRock), $"{profileId}: a peer does not find rock {anyRock.Id} by id.");
            Assert.Less((anyRock.Center - peerRock.Center).magnitude, POSITION_TOLERANCE);
        }

        [Test]
        public void WhenARockIsBroken_ThenTheQueryLeavesItOut() {
            FieldRockTestRig rig = new(_composition, _durability, GetField(GIANTS_PROFILE));
            FieldRockDescription rock = FindBreakableRock(rig, GetField(GIANTS_PROFILE));

            rig.Breaker.ServerBreak(rock.Id, FieldRockDestroyCause.ShipCollision, new PlayerKey(string.Empty));

            List<FieldRockDescription> found = new();
            rig.Query.FindInSphere(rock.Center, rock.Radius, found);
            Assert.IsFalse(found.Any(other => other.Id.Equals(rock.Id)), "A broken rock is still found under a volume.");
            Assert.IsFalse(rig.Query.TryGetRock(rock.Id, out _), "A broken rock is still found by its id.");
        }

        [Test]
        public void WhenTheSameRockIsBrokenTwice_ThenRockDestroyedFiresOnceWithTheRocksData() {
            FlightField field = GetField(GIANTS_PROFILE);
            FieldRockTestRig rig = new(_composition, _durability, field);
            FieldRockDescription rock = FindBreakableRock(rig, field);
            List<FieldRockDestroyed> events = new();
            rig.Breaker.RockDestroyed += events.Add;
            PlayerKey instigator = new("client:test");

            FieldRockBreakResult first = rig.Breaker.ServerBreak(rock.Id, FieldRockDestroyCause.Weapon, instigator);
            FieldRockBreakResult second = rig.Breaker.ServerBreak(rock.Id, FieldRockDestroyCause.ShipCollision, new PlayerKey(string.Empty));

            Assert.AreEqual(FieldRockBreakResult.Broken, first);
            Assert.AreEqual(FieldRockBreakResult.AlreadyBroken, second);
            Assert.AreEqual(1, events.Count, "RockDestroyed must fire once per rock.");
            FieldRockDestroyed destroyed = events[0];
            Assert.AreEqual(rock.Id, destroyed.Id);
            Assert.AreEqual(rock.SourceId, destroyed.SourceId);
            Assert.AreEqual(rock.Size, destroyed.Size);
            Assert.AreEqual(rock.Diameter, destroyed.Diameter);
            Assert.Less((rock.Center - destroyed.Position).magnitude, POSITION_TOLERANCE);
            Assert.AreEqual(FieldRockDestroyCause.Weapon, destroyed.Cause);
            Assert.AreEqual(instigator, destroyed.Instigator);
            Assert.AreEqual(FIELD_NUMBER, destroyed.FieldNumber);
            Assert.AreEqual(LOOP, destroyed.LoopIndex);
        }

        [Test]
        public void WhenAGiantIsBroken_ThenItIsRefusedAsUnbreakableAndLarge() {
            FlightField field = GetField(GIANTS_PROFILE);
            FieldRockTestRig rig = new(_composition, _durability, field);
            FieldRockDescription giant = ReadEveryRock(rig, field).First(IsGiant);

            FieldRockBreakResult result = rig.Breaker.ServerBreak(giant.Id, FieldRockDestroyCause.ShipCollision, new PlayerKey(string.Empty));

            Assert.AreEqual(FieldRockBreakResult.Unbreakable, result);
            Assert.AreEqual(FieldRockSize.Large, giant.Size);
            Assert.IsFalse(rig.BrokenRocks.IsBroken(giant.Id));
        }

        [Test]
        public void WhenTheFieldIsReset_ThenItsBrokenRocksAreForgotten() {
            FlightField field = GetField(GIANTS_PROFILE);
            FieldRockTestRig rig = new(_composition, _durability, field);
            FieldRockDescription rock = FindBreakableRock(rig, field);
            List<FieldRockDestroyed> events = new();
            rig.Breaker.RockDestroyed += events.Add;
            rig.Breaker.ServerBreak(rock.Id, FieldRockDestroyCause.ShipCollision, new PlayerKey(string.Empty));

            rig.Breaker.ServerReset();

            Assert.AreEqual(0, rig.BrokenRocks.Count, "The broken list outlived its field.");
            Assert.IsTrue(rig.Query.TryGetRock(rock.Id, out _), "The rock of a new field must be found again.");
            Assert.AreEqual(FieldRockBreakResult.Broken, rig.Breaker.ServerBreak(rock.Id, FieldRockDestroyCause.ShipCollision, new PlayerKey(string.Empty)));
            Assert.AreEqual(2, events.Count);
        }

        [Test]
        public void WhenTheFieldIsTakenDown_ThenNothingIsFoundAndNothingBreaks() {
            FlightField field = GetField(GIANTS_PROFILE);
            FieldRockTestRig rig = new(_composition, _durability, field);
            FieldRockDescription rock = FindBreakableRock(rig, field);

            rig.ServerModel.ClearField();

            List<FieldRockDescription> found = new();
            rig.Query.FindInSphere(rock.Center, PROBE_RADIUS, found);
            Assert.IsEmpty(found);
            Assert.AreEqual(FieldRockBreakResult.NoField, rig.Breaker.ServerBreak(rock.Id, FieldRockDestroyCause.ShipCollision, new PlayerKey(string.Empty)));
        }

        private FlightField GetField(string profileId) {
            if (_fieldsByProfile.TryGetValue(profileId, out FlightField field))
                return field;

            FieldDifficultyProfile profile = _composition.ProfileLookup.GetRequired(profileId);
            field = _composition.Generator.Generate(FieldTestRequests.WithProfile(_scenario.CreateRequest(_composition.RequestFactory, SEED, LOOP), profile));
            _fieldsByProfile.Add(profileId, field);
            return field;
        }

        // Points on the flight plane along the middle of the field and both sides of it.
        private static List<Vector3> CreateProbes(FieldFrame frame) {
            List<Vector3> probes = new();
            for (int along = 0; along < frame.LengthCells; along += PROBE_STEP_CELLS) {
                probes.Add(frame.CellToWorld(new Vector2Int(along, frame.WidthCells / 2)));
                probes.Add(frame.CellToWorld(new Vector2Int(along, frame.WidthCells / 4)));
                probes.Add(frame.CellToWorld(new Vector2Int(along, frame.WidthCells * 3 / 4)));
            }

            return probes;
        }

        private static Dictionary<FieldRockId, FieldRockDescription> FindAll(FieldRockTestRig rig, List<Vector3> probes, Vector3 shift) {
            Dictionary<FieldRockId, FieldRockDescription> rocks = new();
            List<FieldRockDescription> found = new();
            foreach (Vector3 probe in probes) {
                rig.Query.FindInSphere(probe + shift, PROBE_RADIUS, found);
                foreach (FieldRockDescription rock in found)
                    rocks[rock.Id] = rock;
            }

            return rocks;
        }

        private static void AssertSameRocks(Dictionary<FieldRockId, FieldRockDescription> expected, Dictionary<FieldRockId, FieldRockDescription> actual, Vector3 shift,
                                            string label) {
            CollectionAssert.AreEquivalent(expected.Keys, actual.Keys, $"{label}: the two reads found other rocks.");
            foreach (KeyValuePair<FieldRockId, FieldRockDescription> pair in expected) {
                FieldRockDescription other = actual[pair.Key];
                Assert.Less((pair.Value.Center + shift - other.Center).magnitude, POSITION_TOLERANCE, $"{label}: rock {pair.Key} stands elsewhere.");
                Assert.AreEqual(pair.Value.Diameter, other.Diameter, $"{label}: rock {pair.Key} has another size.");
                Assert.AreEqual(pair.Value.Size, other.Size);
                Assert.AreEqual(pair.Value.IsBreakable, other.IsBreakable);
            }
        }

        private static void AssertSameIds(IEnumerable<FieldRockDescription> expected, List<FieldRockDescription> actual, string label) =>
            CollectionAssert.AreEquivalent(expected.Select(rock => rock.Id).ToList(), actual.Select(rock => rock.Id).ToList(), $"{label}: the query missed or added rocks.");

        private List<FieldRockDescription> ReadEveryRock(FieldRockTestRig rig, FlightField field) {
            FieldRockFieldData fieldData = new(new FieldDebrisWorkspace(new FieldRandomFactory().Create(0)));
            fieldData.Read(field, FIELD_NUMBER, new MatrixFieldDebrisCells(field.Cells, _composition.Catalog, _composition.ProfileLookup.GetRequired(field.ProfileId)));
            List<FieldRockDescription> everyRock = new();
            List<FieldRockDescription> rocks = new();
            for (int placementIndex = 0; placementIndex < field.Placements.Count; placementIndex++) {
                rig.Reader.ReadPlacement(fieldData, field.Frame, placementIndex, rocks);
                everyRock.AddRange(rocks);
            }

            foreach (Vector2Int cell in DebrisCells(field)) {
                rig.Reader.ReadDebrisCell(fieldData, field.Frame, cell, rocks);
                everyRock.AddRange(rocks);
            }

            return everyRock;
        }

        private FieldRockDescription FindBreakableRock(FieldRockTestRig rig, FlightField field) =>
            ReadEveryRock(rig, field).First(rock => rock.IsBreakable);

        private bool IsGiant(FieldRockDescription rock) =>
            rock.Id.Source == FieldRockSource.Cluster && _composition.Catalog.GetRequired(rock.SourceId).ClusterRecipe.IsAlwaysLarge;

        private static IEnumerable<Vector2Int> DebrisCells(FlightField field) {
            for (int index = 0; index < field.Frame.LengthCells * field.Frame.WidthCells; index++) {
                Vector2Int cell = new(index / field.Frame.WidthCells, index % field.Frame.WidthCells);
                if (field.Cells[cell].HasDebris)
                    yield return cell;
            }
        }

        private RecordingFieldPublishTarget Publish(FlightField field) {
            RecordingFieldPublishTarget target = new();
            FieldPublishJob job = new(field, FIELD_NUMBER, _composition.Configuration.PublishEntriesPerBatch);
            while (!job.IsDone)
                job.WriteNextBatch(target);

            return target;
        }
    }
}
