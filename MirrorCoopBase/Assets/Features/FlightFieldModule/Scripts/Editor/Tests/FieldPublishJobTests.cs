using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // A long field goes into the live field model in batches: no batch over the batch size, every entry once and in
    // order, the state with the field number alone after the last entry.
    public sealed class FieldPublishJobTests {
        private const int SEED = 41;
        private const int NUMBER = 7;
        private const int ENTRIES_PER_BATCH = 300;
        // Loop 4 at 120 s + 120 s per loop and 14 m/s: over a thousand placements, about 28 KB.
        private const float FLIGHT_DISTANCE = 8400f;
        private const float TAKEOFF_FORWARD = 350f;
        private const float TAKEOFF_HEIGHT = 100f;
        private const int MAX_BATCHES = 100;

        private FlightField _field;

        [OneTimeSetUp]
        public void SetUp() {
            FlightFieldEditorComposition composition = new FlightFieldEditorCompositionFactory().Create();
            Vector3 cruiseStart = Vector3.forward * TAKEOFF_FORWARD + Vector3.up * TAKEOFF_HEIGHT;
            _field = composition.Generator.Generate(composition.RequestFactory.Create(Vector3.zero, Vector3.forward * FLIGHT_DISTANCE, cruiseStart, 0, SEED));
        }

        [Test]
        public void WhenLongFieldIsPublished_ThenBatchesStayInSizeAndStateComesAloneAfterEveryEntry() {
            FieldPublishJob job = new(_field, NUMBER, ENTRIES_PER_BATCH);
            RecordingTarget target = new();
            int batches = 0;
            while (!job.IsDone && batches < MAX_BATCHES) {
                target.BeginBatch();
                job.WriteNextBatch(target);
                batches++;
                Assert.LessOrEqual(target.EntriesInBatch, ENTRIES_PER_BATCH, $"Batch {batches} is too large.");
                if (target.StateWrites > 0)
                    Assert.AreEqual(0, target.EntriesInBatch, "The state went out in a batch with entries.");
            }

            Assert.IsTrue(job.IsDone, "The publish did not end.");
            Assert.Greater(batches, 2, "A long field went out in one or two batches.");
            Assert.AreEqual(1, target.StateWrites);
            Assert.AreEqual(NUMBER, target.State.Number);
            Assert.AreEqual(_field.Frame.LengthCells, target.State.LengthCells);
            Assert.AreEqual(_field.Placements.Count, target.Placements.Count);
            for (int i = 0; i < _field.Placements.Count; i++)
                Assert.AreEqual(_field.Placements[i].Anchor, target.Placements[i].Anchor, $"Placement {i} is out of order.");

            Assert.AreEqual(_field.Routes.Count, target.Routes);
            Assert.AreEqual(CountRouteCells(), target.RouteCells);

            target.BeginBatch();
            job.WriteNextBatch(target);
            Assert.AreEqual(0, target.EntriesInBatch);
            Assert.AreEqual(1, target.StateWrites, "A finished publish wrote again.");
        }

        [Test]
        public void WhenPublishIsDroppedHalfway_ThenNoStateWasWritten() {
            FieldPublishJob job = new(_field, NUMBER, ENTRIES_PER_BATCH);
            RecordingTarget target = new();
            job.WriteNextBatch(target);
            job.WriteNextBatch(target);

            Assert.IsFalse(job.IsDone);
            Assert.AreEqual(0, target.StateWrites, "Peers would see a new field number with half of its placements.");
        }

        [Test]
        public void WhenWorldShiftsDuringThePublish_ThenTheStateCarriesTheShiftedOrigin() {
            Vector3 shift = new(-800f, 0f, 300f);
            FieldPublishJob job = new(_field, NUMBER, ENTRIES_PER_BATCH);
            RecordingTarget target = new();
            job.WriteNextBatch(target);
            job.ShiftOrigin(shift);
            for (int batch = 0; batch < MAX_BATCHES && !job.IsDone; batch++)
                job.WriteNextBatch(target);

            Assert.AreEqual(_field.Frame.Origin + shift, target.State.Origin);
        }

        private int CountRouteCells() {
            int cells = 0;
            foreach (FieldRoute route in _field.Routes)
                cells += route.Centerline.Count;

            return cells;
        }

        private sealed class RecordingTarget : IFieldPublishTarget {
            public List<FieldPlacementEntry> Placements { get; } = new();
            public int Routes { get; private set; }
            public int RouteCells { get; private set; }
            public int StateWrites { get; private set; }
            public LiveFieldState State { get; private set; }
            public int EntriesInBatch { get; private set; }

            public void BeginBatch() =>
                EntriesInBatch = 0;

            public void AddPlacement(FieldPlacementEntry placement) {
                Placements.Add(placement);
                EntriesInBatch++;
            }

            public void AddRoute(FieldRouteEntry route) {
                Routes++;
                EntriesInBatch++;
            }

            public void AddRouteCell(FieldRouteCellEntry routeCell) {
                RouteCells++;
                EntriesInBatch++;
            }

            public void SetState(LiveFieldState state) {
                State = state;
                StateWrites++;
            }
        }
    }
}
