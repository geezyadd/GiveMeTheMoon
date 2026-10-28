#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Debug;
using Features.FieldAgentModule.Scripts.Network.Generated;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode.FieldAgent {
    // 50 agents in a slow cruise (the debug window's flock plus 30 more): none comes into a rock, the server's agent
    // tick stays within its budget, and the batch size and rate give the traffic per client (logged).
    public sealed class FieldAgentLoadTest {
        private const int TEST_TIMEOUT_MS = 240000;
        private const int MEASURED_AGENTS = 50;
        private const float SETTLE_SECONDS = 2f;
        private const float MEASURE_SECONDS = 5f;
        // The whole agent pass of one server frame, averaged: well under a millisecond measured in the editor.
        private const double TICK_BUDGET_MILLISECONDS = 2d;
        private const float BYTES_PER_KIBIBYTE = 1024f;

        private FieldAgentTestFlight _flight;
        private FieldAgentFlockSteps _steps;

        [SetUp]
        public void SetUp() {
            _flight = new FieldAgentTestFlight();
            _steps = new FieldAgentFlockSteps(_flight);
        }

        [TearDown]
        public void TearDown() =>
            _flight.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenFiftyAgentsFlyInCruise_AndTheServerSendsThemInBatches_ThenTheTickStaysInBudgetAndNoAgentHitsARock() {
            LogAssert.ignoreFailingMessages = true;
            yield return _steps.SpawnFlockInSlowCruiseCoroutine();
            SessionQueries.Resolve<IFieldAgentFlockDebug>().DebugSpawnTestFlock(MEASURED_AGENTS - FieldAgentFlockSteps.FLOCK_SIZE);
            Assert.AreEqual(MEASURED_AGENTS, _flight.Control.Count);
            yield return new WaitForSeconds(SETTLE_SECONDS);
            yield return MeasureCoroutine();
            yield return _flight.LandAndLeaveCoroutine();
        }

        private IEnumerator MeasureCoroutine() {
            FieldAgentDiagnosticsModel diagnostics = SessionQueries.Resolve<FieldAgentDiagnosticsModel>();
            diagnostics.ResetMeasurements();
            float start = Time.time;
            List<int> all = new();
            while (Time.time - start < MEASURE_SECONDS) {
                all.Clear();
                all.AddRange(_steps.Flock.FlockIds);
                _flight.CheckRocks(all);
                yield return null;
            }

            Assert.AreEqual(0, _flight.RockHits, $"An agent flew into a rock with {MEASURED_AGENTS} agents: {_flight.FirstRockHit}.");
            Assert.AreEqual(0, _flight.RockPushOuts, "The steering let agents into rock spheres: the server had to push them out.");
            int batchBytes = MeasureBatchBytes();
            float batchesPerSecond = diagnostics.BatchesSent / (Time.time - start);
            Debug.Log($"{FieldAgentFlockSteps.LOG_PREFIX}{MEASURED_AGENTS} agents: server tick avg {diagnostics.AverageTickMilliseconds:0.000} ms, max {diagnostics.MaxTickMilliseconds:0.000} ms; batch {batchBytes} B x {batchesPerSecond:0.0}/s = {batchBytes * batchesPerSecond / BYTES_PER_KIBIBYTE:0.0} KiB/s per client (payload, before Mirror's message header)");
            Assert.Less(diagnostics.AverageTickMilliseconds, TICK_BUDGET_MILLISECONDS, "The server's agent tick is over its budget.");
        }

        private static int MeasureBatchBytes() {
            using NetworkWriterPooled writer = NetworkWriterPool.Get();
            writer.Write(SessionQueries.Resolve<IReadOnlyFieldAgentsModel>().Frame);
            return writer.Position;
        }
    }
}
#endif
