#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NUnit.Framework;

namespace Tests.PlayMode.LoopSoak {
    // Collects a row per loop, prints the table and checks memory growth. Loop 1 is warm-up: memory is compared
    // between loop 2 and the last loop.
    public sealed class LoopSoakReport {
        private const double MAX_MEMORY_GROWTH = 1.5;
        private const double BYTES_PER_MEGABYTE = 1024d * 1024d;
        private const int BASELINE_LOOP_INDEX = 1;
        private const string HEADER =
            "loop | s    | managed MB | allocated MB | server | client | items (+new) | pads | other srv/cli | shifts | ship m from 0 | field placements | field objects | field pooled";

        private readonly List<LoopSoakSample> _samples = new List<LoopSoakSample>();

        public void Add(LoopSoakSample sample) =>
            _samples.Add(sample);

        public string Describe(float totalSeconds) {
            StringBuilder table = new StringBuilder();
            table.AppendLine($"[LoopSoak] {_samples.Count} loops in {totalSeconds:F0} s");
            table.AppendLine(HEADER);
            int previousItems = 0;
            foreach (LoopSoakSample sample in _samples) {
                table.AppendLine(FormatRow(sample, previousItems));
                previousItems = sample.Census.Items;
            }

            return table.ToString();
        }

        public string DescribeLast() {
            int previousItems = _samples.Count > 1 ? _samples[_samples.Count - 2].Census.Items : 0;
            return $"[LoopSoak] {HEADER}\n[LoopSoak] {FormatRow(_samples[_samples.Count - 1], previousItems)}";
        }

        public void AssertMemoryDoesNotGrow() {
            Assert.Greater(_samples.Count, BASELINE_LOOP_INDEX + 1, "Too few loops to compare memory after the warm-up loop.");
            LoopSoakSample baseline = _samples[BASELINE_LOOP_INDEX];
            LoopSoakSample last = _samples[_samples.Count - 1];
            AssertGrowth("Managed memory", baseline.ManagedBytes, last.ManagedBytes, last.Loop);
            AssertGrowth("Allocated memory", baseline.AllocatedBytes, last.AllocatedBytes, last.Loop);
        }

        private static void AssertGrowth(string what, long baselineBytes, long lastBytes, int lastLoop) {
            double growth = (double)lastBytes / baselineBytes;
            Assert.LessOrEqual(growth, MAX_MEMORY_GROWTH, $"Loop {lastLoop}: {what} grew {growth:F2} times since loop {BASELINE_LOOP_INDEX + 1} ({baselineBytes} -> {lastBytes} bytes).");
        }

        private static string FormatRow(LoopSoakSample sample, int previousItems) {
            LoopSoakSpawnCensus census = sample.Census;
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0,4} | {1,4:F1} | {2,10:F1} | {3,12:F1} | {4,6} | {5,6} | {6,5} ({7,3:+0;-0;0}) | {8,4} | {9,6}/{10,-6} | {11,6} | {12,13:F0} | {13,16} | {14,13} | {15,12}",
                sample.Loop,
                sample.Seconds,
                sample.ManagedBytes / BYTES_PER_MEGABYTE,
                sample.AllocatedBytes / BYTES_PER_MEGABYTE,
                census.ServerSpawned,
                census.ClientSpawned,
                census.Items,
                census.Items - previousItems,
                census.Pads,
                census.ServerOther,
                census.ClientOther,
                sample.WorldShifts,
                sample.ShipDistanceFromOrigin,
                sample.FieldPlacements,
                sample.FieldObjects,
                sample.FieldPooledObjects);
        }
    }
}
#endif
