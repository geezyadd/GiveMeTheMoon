#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using Features.ShipModule.Scripts;
using NUnit.Framework;
using Tests.PlayMode.LoopSmoke;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

namespace Tests.PlayMode.LoopSoak {
    // Flies the loop many times in a row on a host and catches what only shows up over time: objects piling up,
    // drift after world shifts, memory growth. LoopSmokeTest covers the features of a station; this test does not
    // repeat them.
    [Category("Soak")]
    public sealed class LoopSoakTest {
        private const int LOOPS = 15;
        // Menu, Host, lobby and the GameScene load; leaving to the menu.
        private const int SESSION_BUDGET_SECONDS = 150;
        // A loop on fast timings takes about 20 s (takeoff, the cruise distance at cruise speed, landing, checks).
        private const int LOOP_BUDGET_SECONDS = 60;
        private const int TEST_TIMEOUT_MS = (SESSION_BUDGET_SECONDS + LOOPS * LOOP_BUDGET_SECONDS) * 1000;

        private LoopSmokeErrorLog _errorLog;
        private LoopSmokeSteamLobbySpy _steamLobby;
        private LoopSmokeShipRunConfigOverride _configOverride;
        private LoopSmokeSession _session;
        private LoopSmokeFlight _flight;

        [SetUp]
        public void SetUp() {
            _configOverride = new LoopSmokeShipRunConfigOverride(LoopSmokeShipRunConfigOverride.FAST_TIMINGS_JSON);
            _errorLog = new LoopSmokeErrorLog();
            _steamLobby = new LoopSmokeSteamLobbySpy();
            _steamLobby.BindIntoSceneContexts();
            _session = new LoopSmokeSession(_errorLog);
            _flight = new LoopSmokeFlight(_session);
        }

        [TearDown]
        public void TearDown() {
            _configOverride.Dispose();
            _errorLog.Dispose();
            _steamLobby.Dispose();
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHostFliesFifteenLoops_ThenNothingPilesUpAndMemoryStaysFlat() {
            // LoopSmokeErrorLog fails the test with the full list and its allow-list instead.
            LogAssert.ignoreFailingMessages = true;
            float testStart = Time.realtimeSinceStartup;
            yield return _session.EnterLobbyAsHostCoroutine();
            yield return _session.StartGameCoroutine();
            _configOverride.AssertBound();
            _flight.InstallRequiredEngines();
            yield return _flight.BoardPlayerCoroutine();

            LoopSoakReport report = new LoopSoakReport();
            LoopSoakAccumulationCheck accumulation = new LoopSoakAccumulationCheck();
            LoopSoakWorldShiftCheck worldShift = new LoopSoakWorldShiftCheck(_configOverride.Config);
            for (int loop = 1; loop <= LOOPS; loop++)
                yield return FlyCheckedLoopCoroutine(loop, report, accumulation, worldShift);

            Debug.Log(report.Describe(Time.realtimeSinceStartup - testStart));
            Assert.Greater(worldShift.Count, 0, $"No world shift happened in {LOOPS} loops: the checks after a shift proved nothing.");
            report.AssertMemoryDoesNotGrow();
            yield return _session.LeaveToMenuCoroutine();
            Assert.AreEqual(0, _errorLog.Count, $"Errors were logged during the loops:\n{_errorLog.Describe()}");
        }

        private IEnumerator FlyCheckedLoopCoroutine(int loop, LoopSoakReport report, LoopSoakAccumulationCheck accumulation, LoopSoakWorldShiftCheck worldShift) {
            float loopStart = Time.realtimeSinceStartup;
            LoopSoakFlightFieldCheck field = new LoopSoakFlightFieldCheck();
            ShipLandingPad launchPad = _flight.FindPadUnderShip();
            yield return _flight.FlyLoopCoroutine(loop, () => field.CheckInCruiseCoroutine(loop));
            yield return _flight.AssertLandedSafelyCoroutine(loop, launchPad, _flight.FindPadUnderShip());
            worldShift.AssertAfterLanding(loop, _session.Ship, _flight);
            field.CheckAfterLanding(loop);
            LoopSoakSpawnCensus census = LoopSoakSpawnCensus.Take();
            accumulation.AssertNothingPiledUp(loop, census);
            report.Add(new LoopSoakSample(
                loop,
                Time.realtimeSinceStartup - loopStart,
                GC.GetTotalMemory(true),
                Profiler.GetTotalAllocatedMemoryLong(),
                census,
                worldShift.Count,
                LoopSoakWorldShiftCheck.DistanceFromOrigin(_session.Ship),
                field));
            Debug.Log(report.DescribeLast());
        }
    }
}
#endif
