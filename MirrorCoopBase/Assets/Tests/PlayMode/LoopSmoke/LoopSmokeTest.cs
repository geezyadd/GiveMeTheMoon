#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using Features.ShipModule.Scripts;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests.PlayMode.LoopSmoke {
    // Plays the runs below on a host and runs the registered checks (LoopSmokeCheckRegistry) at every slot on the way:
    // a new check registers its own [LoopSmokeStep] methods and does not edit this file.
    public sealed class LoopSmokeTest {
        private const int TEST_TIMEOUT_MS = 240000;

        private static readonly LoopSmokeRun[] _runs = {
            new LoopSmokeRun(2, true, LoopSmokeRunEnd.AllDead),
            // Five flights in a row: the field pool and the Mirror field objects must not grow. The sixth is lost in
            // its cruise (LoopSmokeHullLossCheck).
            new LoopSmokeRun(6, true, LoopSmokeRunEnd.HullLossInCruise),
            // The Late stage preset (LoopSmokeStagePresetCheck) brings its own engines.
            new LoopSmokeRun(1, false, LoopSmokeRunEnd.AllDead)
        };

        private LoopSmokeErrorLog _errorLog;
        private LoopSmokeSteamLobbySpy _steamLobby;
        private LoopSmokeShipRunConfigOverride _configOverride;
        private LoopSmokeFieldCatalogOverride _fieldCatalogOverride;
        private LoopSmokeSonarConfigOverride _sonarConfigOverride;
        private LoopSmokeSession _session;
        private LoopSmokeContext _context;
        private LoopSmokeCheckRegistry _checks;

        private LoopSmokeFlight Flight =>
            _context.Flight;

        [SetUp]
        public void SetUp() {
            _configOverride = new LoopSmokeShipRunConfigOverride(LoopSmokeShipRunConfigOverride.FAST_TIMINGS_JSON);
            _fieldCatalogOverride = new LoopSmokeFieldCatalogOverride();
            _sonarConfigOverride = new LoopSmokeSonarConfigOverride();
            _errorLog = new LoopSmokeErrorLog();
            _steamLobby = new LoopSmokeSteamLobbySpy();
            _steamLobby.BindIntoSceneContexts();
            _session = new LoopSmokeSession(_errorLog);
            _context = new LoopSmokeContext(_session, _steamLobby);
            _checks = LoopSmokeCheckRegistry.CreateFromAssembly(_context);
        }

        [TearDown]
        public void TearDown() {
            _configOverride.Dispose();
            _fieldCatalogOverride.Dispose();
            _sonarConfigOverride.Dispose();
            _errorLog.Dispose();
            _steamLobby.Dispose();
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHostFliesTwoLoops_AndAllDieAndStartAgain_ThenSecondRunStartsCleanAndMenuReturns() {
            // LoopSmokeErrorLog fails the test with the full list and its allow-list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return _session.EnterLobbyAsHostCoroutine();
            for (int run = 1; run <= _runs.Length; run++)
                yield return PlayRunCoroutine(run, _runs[run - 1]);

            yield return _session.LeaveToMenuCoroutine();
            yield return _checks.RunSlotCoroutine(LoopSmokeSlot.InMenu());
            _checks.AssertEveryStepRan();
            Assert.AreEqual(0, _errorLog.Count, $"Errors were logged during the loop:\n{_errorLog.Describe()}");
        }

        private IEnumerator PlayRunCoroutine(int run, LoopSmokeRun plan) {
            yield return _checks.RunSlotCoroutine(LoopSmokeSlot.OfRun(LoopSmokePhase.InLobby, run));
            yield return _session.StartGameCoroutine();
            if (run == 1)
                StartFirstRun();

            if (plan.HasTestEngines)
                Flight.InstallRequiredEngines();

            yield return Flight.BoardPlayerCoroutine();
            int startLoopIndex = _session.Model.LoopIndex;
            for (int flight = 1; flight <= plan.Flights; flight++)
                yield return PlayFlightCoroutine(run, flight, startLoopIndex + flight, plan);

            yield return EndRunCoroutine(plan.End);
            yield return _checks.RunSlotCoroutine(LoopSmokeSlot.OfRun(LoopSmokePhase.AfterRun, run));
        }

        private void StartFirstRun() {
            _configOverride.AssertBound();
            _context.CreateFlight();
        }

        private IEnumerator PlayFlightCoroutine(int run, int flight, int loopIndex, LoopSmokeRun plan) {
            yield return _checks.RunSlotCoroutine(LoopSmokeSlot.OfFlight(LoopSmokePhase.BeforeTakeoff, run, flight));
            Func<IEnumerator> inCruise = () => _checks.RunSlotCoroutine(LoopSmokeSlot.OfFlight(LoopSmokePhase.InCruise, run, flight));
            if (plan.End == LoopSmokeRunEnd.HullLossInCruise && flight == plan.Flights) {
                yield return Flight.FlyToCruiseCoroutine(loopIndex, inCruise);
                yield break;
            }

            ShipLandingPad launchPad = Flight.FindPadUnderShip();
            yield return Flight.FlyLoopCoroutine(loopIndex, inCruise);
            if (plan.HasTestEngines)
                yield return Flight.AssertLandedSafelyCoroutine(loopIndex, launchPad, Flight.FindPadUnderShip());

            yield return _checks.RunSlotCoroutine(LoopSmokeSlot.OfFlight(LoopSmokePhase.AfterLanding, run, flight));
        }

        private IEnumerator EndRunCoroutine(LoopSmokeRunEnd end) {
            switch (end) {
                case LoopSmokeRunEnd.AllDead:
                    yield return _session.ReturnToLobbyByAllDeadCoroutine();
                    yield break;
                case LoopSmokeRunEnd.HullLoss:
                case LoopSmokeRunEnd.HullLossInCruise:
                    yield return _session.ReturnToLobbyByHullLossCoroutine();
                    yield break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(end), end, "A loop test run has no end.");
            }
        }
    }
}
#endif
