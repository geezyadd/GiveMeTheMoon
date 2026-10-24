using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // Counts what a publish job writes, entry by entry and batch by batch.
    public sealed class RecordingFieldPublishTarget : IFieldPublishTarget {
        public List<FieldPlacementEntry> Placements { get; } = new();
        public List<FieldDebrisRunEntry> DebrisRuns { get; } = new();
        public int Routes { get; private set; }
        public int RouteCells { get; private set; }
        public int DebrisCells { get; private set; }
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

        public void AddDebrisRun(FieldDebrisRunEntry debrisRun) {
            DebrisRuns.Add(debrisRun);
            DebrisCells += debrisRun.Length;
            EntriesInBatch++;
        }

        public void SetState(LiveFieldState state) {
            State = state;
            StateWrites++;
        }
    }
}
