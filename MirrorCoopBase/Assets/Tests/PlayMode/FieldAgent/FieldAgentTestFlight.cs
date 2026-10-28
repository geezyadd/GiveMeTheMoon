#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Features.DebugModule.Contracts;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Server;
using Features.FlightFieldModule.Contracts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using Features.ShipModule.Scripts.Generated;
using NUnit.Framework;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using Zenject;

namespace Tests.PlayMode.FieldAgent {
    // The shared part of the field agent tests: a host session on the loop tests' fast timings, flown into the first
    // cruise and kept there (the ship faces away from the station), the debug window's agent buttons clicked as a
    // player does, and the check that no agent ever comes into a rock sphere of the field data.
    public sealed class FieldAgentTestFlight : IDisposable {
        private const float CRUISE_TIMEOUT_SECONDS = 40f;
        private const float LANDING_TIMEOUT_SECONDS = 60f;
        private const float TURN_AWAY_SECONDS = 4f;
        private const float FULL_THROTTLE = 1f;
        // The workspace's screenshot folder, next to the Unity project.
        private const string SCREENSHOT_FOLDER = "../.agent-orchestrator/screenshots/field-agents";

        private readonly LoopSmokeErrorLog _errorLog = new();
        private readonly LoopSmokeSteamLobbySpy _steamLobby = new();
        private readonly LoopSmokeShipRunConfigOverride _configOverride = new(LoopSmokeShipRunConfigOverride.FAST_TIMINGS_JSON);
        private readonly LoopSmokeFieldCatalogOverride _fieldCatalogOverride = new();
        private readonly List<FieldRockDescription> _touching = new();
        private readonly HashSet<FieldRockId> _rocksNear = new();
        private LoopSmokeFlight _flight;

        public LoopSmokeSession Session { get; }
        public ShipBase Ship => Session.Ship;
        public IFieldAgentControl Control { get; private set; }
        public IFieldRockQuery Rocks { get; private set; }
        public FieldAgentProfile Profile { get; private set; }
        public int RockHits { get; private set; }
        // The server's last-resort pushes out of a rock: the steering itself must keep agents out, so these stay 0.
        public int RockPushOuts => SessionQueries.Resolve<FieldAgentDiagnosticsModel>().RockPushOuts;
        public int RocksPassedNear => _rocksNear.Count;
        public string FirstRockHit { get; private set; }

        public FieldAgentTestFlight() {
            _steamLobby.BindIntoSceneContexts();
            Session = new LoopSmokeSession(_errorLog);
        }

        public void Dispose() {
            _configOverride.Dispose();
            _fieldCatalogOverride.Dispose();
            _errorLog.Dispose();
            _steamLobby.Dispose();
        }

        // atStation: a step at the station before boarding (buying something at the kiosk), none by default.
        public IEnumerator FlyIntoEndlessCruiseCoroutine(Func<LoopSmokeFlight, IEnumerator> atStation = null) {
            yield return Session.EnterLobbyAsHostCoroutine();
            yield return Session.StartGameCoroutine();
            _configOverride.AssertBound();
            _flight = new LoopSmokeFlight(Session, new LoopSmokeFieldCheck(ProjectContext.Instance.Container));
            _flight.InstallRequiredEngines();
            if (atStation != null)
                yield return atStation(_flight);

            yield return _flight.BoardPlayerCoroutine();
            Control = SessionQueries.Resolve<IFieldAgentControl>();
            Rocks = SessionQueries.Resolve<IFieldRockQuery>();
            Profile = SessionQueries.Resolve<FieldAgentConfiguration>().Profiles[0];
            Session.StartPhaseRecord();
            _flight.PullLaunchLever(1);
            yield return null;
            // The fast test flight's station is only seconds of cruise away: the ship stops, turns away from it and only
            // then flies on (each test sets its own throttle), so the cruise lasts as long as the test needs.
            SetThrottle(0f);
            yield return Session.WaitForCoroutine(IsCruising, CRUISE_TIMEOUT_SECONDS, "the first cruise");
            FaceAwayFromStation();
            yield return new WaitForSeconds(TURN_AWAY_SECONDS);
        }

        public IEnumerator LandAndLeaveCoroutine() {
            Ship.FlightControlDebug.SetDebugSteer(false, 0f);
            SetThrottle(FULL_THROTTLE);
            _flight.FaceDestination();
            yield return Session.WaitForCoroutine(IsBackInBuild, LANDING_TIMEOUT_SECONDS, "Build after the landing");
            yield return Session.LeaveToMenuCoroutine();
            Assert.AreEqual(0, _errorLog.Count, $"Errors were logged:\n{_errorLog.Describe()}");
        }

        public IEnumerator ClickDebugActionCoroutine(string label, string value = null) {
            DebugWindowClicking clicking = new();
            yield return clicking.ClickCoroutine(DebugSections.AGENTS, label, value);
            Assert.IsNull(clicking.Failure, $"Debug window \"{label}\": {clicking.Failure}.");
        }

        public void SetThrottle(float position) =>
            SessionQueries.Resolve<IShipThrottleDebug>().DebugSetThrottle(position);

        public void FaceAwayFromStation() =>
            Ship.FlightControlDebug.DebugFace(Ship.transform.position - Session.Model.TransitDestination);

        // Every flying agent's sphere against the rock spheres of the server's field data.
        public void CheckRocks(IReadOnlyList<int> ids) {
            foreach (int id in ids) {
                if (Control.TryGetAgent(id, out FieldAgentState state) && state.Mode == FieldAgentMode.Flight)
                    CheckAgentRocks(id, state);
            }
        }

        public static IEnumerator CaptureCoroutine(string fileName) {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SCREENSHOT_FOLDER));
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, fileName));
            yield return null;
            yield return null;
        }

        private void CheckAgentRocks(int id, in FieldAgentState state) {
            Rocks.FindInSphere(state.Position, Profile.Radius + Profile.RockClearance + Profile.MinLookAheadMeters, _touching);
            foreach (FieldRockDescription rock in _touching) {
                _rocksNear.Add(rock.Id);
                float gap = Vector3.Distance(state.Position, rock.Center) - rock.Radius - Profile.Radius;
                if (gap >= 0f)
                    continue;

                RockHits++;
                FirstRockHit ??= $"agent {id} at {state.Position} is {-gap:0.00} m inside rock {rock.Id} ({rock.Diameter:0.0} m)";
            }
        }

        private bool IsCruising() =>
            Session.Model.Phase == ShipRunPhase.Cruise;

        private bool IsBackInBuild() =>
            Session.Phases.Count > 1 && Session.Model.Phase == ShipRunPhase.Build;
    }
}
#endif
