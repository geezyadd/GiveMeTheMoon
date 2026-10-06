#if UNITY_EDITOR || DEVELOPMENT_BUILD
// Measurement tool for deck feel (agent QA). Not compiled into release builds.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Features.CameraModule.Scripts;
using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Features.GrabModule.Scripts;
using Game.Connection;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Features.ShipModule.Scripts.Debug {
    public sealed class DeckFeelHarness : ITickable {
        private const string RunDirPrefix = "dir=";
        private const string DefaultRunDir = @"C:\Users\User\.agent-orchestrator\runs\20261001-2325-flight-h";
        private const float SpikeMeters = 0.1f;
        private const int FixedDtFps = 60;
        private const float StandSeconds = 3f;
        private const float WalkSeconds = 3f;
        private const float StrafeSeconds = 2f;
        private const float SprintSeconds = 2f;
        private const float JumpGapSeconds = 1.15f;

        public static int Done;
        public static string Status = "idle";

        private readonly CharacterInputBuffer _input;
        private readonly IGameCameraService _cameras;
        private readonly IConnectionSessionService _connection;
        private readonly IGameFlowStateMachineService _flow;
        private readonly IShipRoute _route;
        private readonly IShipWorldShiftService _worldShift;
        private readonly ShipRunModel _model;
        private readonly ShipStationCatalog _stations;

        private readonly List<Vector3> _player = new List<Vector3>(4096);
        private readonly List<Vector3> _cameraPos = new List<Vector3>(4096);
        private readonly List<Quaternion> _cameraRot = new List<Quaternion>(4096);
        private readonly List<int> _steps = new List<int>(4096);
        private readonly List<float> _dts = new List<float>(4096);
        private readonly List<Vector3> _shipWorld = new List<Vector3>(4096);
        private readonly List<FrameEvents> _events = new List<FrameEvents>(4096);
        private readonly Dictionary<string, List<Vector3>> _dockTraces = new Dictionary<string, List<Vector3>>();
        private readonly StringBuilder _table = new StringBuilder(2048);
        private readonly StringBuilder _report = new StringBuilder(4096);

        private bool _gateChecked;
        private bool _stopped;
        private int _state;
        private float _stateStart;
        private int _measureStep;
        private float _measureTime;
        private Task _host;
        private int _hostFailures;
        private bool _gameStarted;
        private ShipBase _ship;
        private ShipRider _rider;
        private bool _itemSpawned;
        private Grabbable _looseItem;
        private int _warmup;
        private bool _measureReady;
        private bool _approachReady;
        private bool _stationReady;
        private bool _cargoNoted;
        private bool _shiftDock;
        private bool _shiftCruise;
        private int _shiftMarks;
        private int _lastShifts;
        private int _lastGc;
        private int _spikeLines;
        private string _pendingShot;
        private int _pendingShotWait;
        private bool _captureSample;
        private bool _finishAfterSample;
        private bool _finishCruise;
        private string _finishLabel;
        private string _finishCamera;
        private int _finishState;
        private float _dockFpJerkRms;
        private float _dockTpJerkRms;
        private int _flightPass;
        private int _loopsSeen;
        private bool _shotStation;
        private bool _shotApproach;
        private Vector3 _hoverStart;
        private bool _hoverSampled;
        private float _cargoDriftMax;
        private bool _client;
        private bool _fixedDt;
        private bool _awaitClient;
        private string _mode = "realtime";
        private Task _join;
        private int _clientLegs;
        private float _clientReadySince;
        private string _runDir = DefaultRunDir;

        public DeckFeelHarness(
            CharacterInputBuffer input,
            IGameCameraService cameras,
            IConnectionSessionService connection,
            IGameFlowStateMachineService flow,
            IShipRoute route,
            IShipWorldShiftService worldShift,
            ShipRunModel model,
            ShipStationCatalog stations) {
            _input = input;
            _cameras = cameras;
            _connection = connection;
            _flow = flow;
            _route = route;
            _worldShift = worldShift;
            _model = model;
            _stations = stations;
        }

        public void Tick() {
            if (_stopped) {
                if (GameCameraService.AfterPresent == OnAfterPresent)
                    GameCameraService.AfterPresent = null;

                return;
            }

            GameCameraService.AfterPresent = OnAfterPresent;

            if (_gateChecked == false) {
                _gateChecked = true;
                if (File.Exists(RequestPath()) == false) {
                    _stopped = true;
                    return;
                }

                string request = File.ReadAllText(RequestPath());
                _awaitClient = request.Contains("await-client");
                _client = request.Contains("client") && _awaitClient == false;
                _fixedDt = request.Contains("fixeddt");
                _mode = _fixedDt ? "fixeddt" + FixedDtFps : "realtime";
                _runDir = ReadRunDir(request);
                if (_fixedDt)
                    Time.captureFramerate = FixedDtFps;

                Directory.CreateDirectory(Path.Combine(_runDir, "screenshots"));
                Directory.CreateDirectory(Path.Combine(_runDir, "frames"));
                Status = "armed";
                Note("Deck feel harness armed. role=" + Role() + " mode=" + _mode + " request='" + request.Trim() + "'");
                Note("Metrics over ALL frames. raw = |x[i]-2x[i-1]+x[i-2]| (m); tc = time-correct second difference "
                    + "|dx[i]-dx[i-1]*dt[i]/dt[i-1]| (m), zero for any constant velocity whatever the frame times; "
                    + "dt2 = raw/dt^2 (m/s^2). steady = without input-step and jump takeoff/landing frames.");
            }

            if (_client) {
                TickClient();
                return;
            }

            if (_state == 0)
                TickHost();
            else if (_state == 1)
                TickWaitActors();
            else if (_state == 2)
                TickPrepare();
            else if (_state == 3)
                TickMeasure(false, CameraIds.FPCamera, "dock-fp");
            else if (_state == 4)
                TickMeasure(false, CameraIds.TPCamera, "dock-tp");
            else if (_state == 5)
                TickLaunch(false);
            else if (_state == 6)
                TickCruiseShots();
            else if (_state == 7)
                TickMeasure(true, CameraIds.FPCamera, "cruise-fp");
            else if (_state == 8)
                TickMeasure(true, CameraIds.TPCamera, "cruise-tp");
            else if (_state == 9)
                TickFinishLeg(false);
            else if (_state == 10)
                TickLaunch(true);
            else if (_state == 11)
                TickHoverCruise();
            else if (_state == 12)
                TickFinishLeg(true);
        }

        private void TickHost() {
            Status = "host";
            if (NetworkServer.active) {
                Enter(1);
                return;
            }

            if (NetworkServer.active == false && Age() < 2f)
                return;

            if (_host == null) {
                _host = HostAsync();
                return;
            }

            if (_host.IsFaulted) {
                _hostFailures += 1;
                Note("Host failed: " + _host.Exception.GetBaseException().Message);
                _host = null;
                _stateStart = Time.realtimeSinceStartup;
                if (_hostFailures >= 4)
                    Fail("could not host");
            }
            else if (Age() > 60f) {
                Fail("timed out waiting to host");
            }
        }

        private static int ReadyRemoteClients() {
            int ready = 0;
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values) {
                if (connection != NetworkServer.localConnection && connection.isAuthenticated)
                    ready += 1;
            }

            return ready;
        }

        private async Task HostAsync() {
            await _connection.HostAsync();
            await _flow.EnterAsync<SessionGameFlowState>();
        }

        private void TickWaitActors() {
            Status = "wait-actors";
            if (_awaitClient && _gameStarted == false) {
                if (ReadyRemoteClients() == 0)
                    _clientReadySince = Time.realtimeSinceStartup;

                if (Time.realtimeSinceStartup - _clientReadySince < 4f) {
                    if (Age() > 240f)
                        Fail("timed out waiting for the client to join");

                    return;
                }
            }

            if (_ship == null && _gameStarted == false && NetworkServer.active && Age() > 1f) {
                _connection.StartGame();
                _gameStarted = true;
                Note("Start game requested.");
            }

            if (_ship == null)
                _ship = UnityEngine.Object.FindFirstObjectByType<ShipBase>();

            if (_rider == null && _ship != null)
                _rider = FindOwnedRider();

            if (_ship != null && _rider != null && _model.Phase == ShipRunPhase.Build) {
                Enter(2);
                return;
            }

            if (Age() > 90f)
                Fail("timed out waiting for the ship and the local player in " + SceneManager.GetActiveScene().name);
        }

        private void TickPrepare() {
            Status = "prepare";
            InstallRequiredModules();
            SpawnDeckItem();
            if (_rider.IsRiding == false) {
                PlaceRiderOnDeck();
                _ship.Riders.DebugBindRider(_rider);
                if (_rider.IsRiding == false) {
                    Vector3 local = _ship.transform.InverseTransformPoint(_rider.transform.position);
                    Fail("local player did not bind to the deck at " + local.ToString("0.00"));
                    return;
                }
            }

            _input.BeginScripted();
            _cameras.BlendTo(CameraIds.FPCamera, 0f);
            if (_awaitClient && Age() < 5f)
                return;

            Note("Modules: " + ModuleLine());
            Enter(3);
        }

        private void TickLaunch(bool hover) {
            Status = hover ? "launch-hover" : "launch-travel";
            _input.EndScripted();
            _ship.FlightControl.SetDebugSteer(false, 0f);
            if (_ship.CanLaunch == false) {
                InstallRequiredModules();
                if (_ship.CanLaunch == false) {
                    Fail("ship cannot launch. " + ModuleLine());
                    return;
                }
            }

            if (_cargoNoted == false) {
                _cargoNoted = true;
                if (_ship.Cargo.AttachedCount == 0 && _looseItem != null) {
                    _ship.Cargo.DebugAttach(_looseItem, _ship.transform, _ship.PoseSync);
                    Note("Deck scan missed the loose item; pinned it directly. cargo=" + _ship.Cargo.AttachedCount);
                }
                else {
                    Note("Deck cargo attached by scan. cargo=" + _ship.Cargo.AttachedCount);
                }
            }

            if (hover)
                _route.DebugUseFlightMode(ShipFlightMode.HoverInPlace);
            else
                _route.DebugClearFlightMode();

            if (_ship.ServerRequestLaunch() == false) {
                if (Age() > 5f)
                    Fail("launch was rejected");

                return;
            }

            _hoverSampled = false;
            Enter(hover ? 11 : 6);
        }

        private void TickCruiseShots() {
            Status = "cruise-shots";
            if (_model.Phase != ShipRunPhase.Cruise) {
                if (Age() > 40f)
                    Fail("timed out waiting for cruise");

                return;
            }

            if (_flightPass >= 2) {
                if (_shotStation == false)
                    TryCaptureStation();

                if (_shotStation || Age() > 25f)
                    Enter(9);

                return;
            }

            if (_flightPass == 1) {
                Enter(8);
                return;
            }

            if (Age() > 1.5f)
                Enter(7);
        }

        private void TickMeasure(bool cruise, string cameraId, string label) {
            Status = "measure-" + label;
            if (_measureReady == false) {
                if (_warmup <= 0) {
                    StandOnDeck();
                    _input.BeginScripted();
                    _input.SetScripted(Vector2.zero, false, false);
                    if (_cameras.ActiveId != cameraId)
                        _cameras.BlendTo(cameraId, 0f);

                    CameraLookDriver.DebugPitchOverride = cameraId == CameraIds.FPCamera;
                    CameraLookDriver.DebugPitch = 28f;
                    _warmup = 8;
                }

                _warmup -= 1;
                _input.SetScripted(Vector2.zero, false, false);
                if (_warmup > 0)
                    return;

                _measureReady = true;
            }

            if (_client == false && cruise) {
                _ship.FlightControl.SetDebugSteer(true, Mathf.Sin(Time.time * 1.7f) * 0.55f);
                if (_shiftCruise == false && cameraId == CameraIds.FPCamera && _measureStep == 1 && _measureTime > 0.6f) {
                    _shiftCruise = true;
                    _shiftMarks = 3;
                    _worldShift.DebugForceRecenter();
                }
            }
            else if (_client == false && _shiftDock == false && _measureStep == 0 && _measureTime > 1f) {
                _shiftDock = true;
                _shiftMarks = 3;
                _worldShift.DebugForceRecenter();
            }

            ApplyMeasureInput();
            _captureSample = true;
            if (AdvanceMeasureClock() == false)
                return;

            _finishAfterSample = true;
            _finishCruise = cruise;
            _finishLabel = label;
            _finishCamera = cameraId;
            _finishState = _client ? 21 : _state == 7 ? 9 : _state + 1;
        }

        private void OnAfterPresent() {
            if (_captureSample) {
                _captureSample = false;
                RecordSample();
            }

            if (_finishAfterSample == false)
                return;

            _finishAfterSample = false;
            AppendMetrics(_finishLabel);
            if (_finishCruise && _client == false)
                ArmWalkShot(_finishCamera);

            if (_ship != null && _client == false)
                _ship.FlightControl.SetDebugSteer(false, 0f);

            Enter(_finishState);
        }

        private void ArmWalkShot(string cameraId) {
            _pendingShot = cameraId == CameraIds.FPCamera ? "cruise-walk-fp" : "cruise-walk-tp";
            _pendingShotWait = 2;
            _input.SetScripted(new Vector2(0f, 1f), false, false);
        }

        private void TickFinishLeg(bool hoverDone) {
            Status = hoverDone ? "hover-land" : "travel-land";
            if (_pendingShot != null) {
                _input.SetScripted(new Vector2(0f, 1f), false, false);
                if (_pendingShotWait > 0)
                    _pendingShotWait -= 1;
                else {
                    Capture(_pendingShot);
                    _pendingShot = null;
                    _input.SetScripted(Vector2.zero, false, false);
                }
            }

            _ship.FlightControl.SetDebugSteer(false, 0f);
            if (_model.Phase == ShipRunPhase.Cruise || _model.Phase == ShipRunPhase.Takeoff) {
                Vector3 to = _model.TransitDestination - _ship.transform.position;
                _ship.FlightControl.DebugFace(to);
            }

            if (_model.Phase == ShipRunPhase.Landing && _shotApproach == false && hoverDone == false) {
                CameraLookDriver.DebugPitchOverride = true;
                CameraLookDriver.DebugPitch = 12f;
                if (_approachReady) {
                    Capture("approach-pad");
                    _shotApproach = true;
                }

                _approachReady = true;
            }

            if (_model.Phase != ShipRunPhase.Build) {
                if (Age() > 90f)
                    Fail("timed out waiting for landing");

                return;
            }

            _loopsSeen = _model.LoopIndex;
            Note("Landed loop " + _loopsSeen + ". " + ModuleLine() + " cargoDrift=" + CargoDrift().ToString("0.000") + " cargo=" + _ship.Cargo.AttachedCount);
            if (hoverDone) {
                Finish(true);
                return;
            }

            if (_flightPass < 2) {
                _flightPass += 1;
                Enter(5);
                return;
            }

            if (_loopsSeen < 3) {
                Enter(5);
                return;
            }

            Enter(10);
        }

        private void TickHoverCruise() {
            Status = "hover-cruise";
            if (_model.Phase != ShipRunPhase.Cruise) {
                if (Age() > 40f)
                    Fail("timed out waiting for hover cruise");

                return;
            }

            if (_hoverSampled == false) {
                _hoverStart = _ship.transform.position;
                _hoverSampled = true;
                _stateStart = Time.realtimeSinceStartup;
                return;
            }

            if (Age() < 3f)
                return;

            Vector3 delta = _ship.transform.position - _hoverStart;
            delta.y = 0f;
            Note("Hover cruise horizontal move in 3s: " + delta.magnitude.ToString("0.00") + " m (mode " + _ship.FlightControl.Mode + ").");
            if (delta.magnitude > 15f) {
                Fail("hover cruise translated " + delta.magnitude.ToString("0.00") + " m");
                return;
            }

            _route.DebugClearFlightMode();
            Enter(12);
        }

        private void ApplyMeasureInput() {
            Vector2 move = Vector2.zero;
            bool sprint = false;
            bool jump = false;
            if (_measureStep == 1)
                move = new Vector2(0f, 1f);
            else if (_measureStep == 2)
                move = new Vector2(1f, 0f);
            else if (_measureStep == 3) {
                move = new Vector2(0f, -1f);
                sprint = true;
            }
            else if (_measureStep == 4 || _measureStep == 6)
                jump = true;

            _input.SetScripted(move, jump, sprint);
        }

        private bool AdvanceMeasureClock() {
            float duration = StepDuration(_measureStep);
            _measureTime += Time.deltaTime;
            if (_measureTime < duration)
                return false;

            _measureTime = 0f;
            _measureStep += 1;
            return _measureStep > 7;
        }

        private static float StepDuration(int step) {
            if (step == 0)
                return StandSeconds;
            if (step == 1)
                return WalkSeconds;
            if (step == 2)
                return StrafeSeconds;
            if (step == 3)
                return SprintSeconds;
            if (step == 4 || step == 6)
                return 0.08f;

            return JumpGapSeconds;
        }

        private void RecordSample() {
            if (_ship == null || _rider == null)
                return;

            Transform ship = _ship.transform;
            _player.Add(ship.InverseTransformPoint(_rider.transform.position));
            Camera camera = _cameras.OutputCamera;
            if (camera == null) {
                _cameraPos.Add(Vector3.zero);
                _cameraRot.Add(Quaternion.identity);
            }
            else {
                _cameraPos.Add(ship.InverseTransformPoint(camera.transform.position));
                _cameraRot.Add(Quaternion.Inverse(ship.rotation) * camera.transform.rotation);
            }

            _shipWorld.Add(ship.position);
            _steps.Add(_measureStep);
            _dts.Add(Mathf.Max(Time.deltaTime, 0.0001f));
            RecordEvents();
        }

        private void RecordEvents() {
            int shifts = _worldShift.WorldShiftCount;
            int gc = GC.CollectionCount(0);
            int index = _player.Count - 1;
            float poseDrift = (_player[index] - _rider.DebugLocalOffset).magnitude;
            _events.Add(new FrameEvents(
                Time.frameCount,
                _ship.ConsumeFixedSteps(),
                shifts - _lastShifts,
                gc - _lastGc,
                _model.Phase,
                _input.MoveStick,
                _input.SprintHeld,
                _input.JumpHeld,
                poseDrift,
                _ship.transform.eulerAngles.y));
            _lastShifts = shifts;
            _lastGc = gc;
        }

        private void AppendMetrics(string label) {
            Vector3 rest = AveragePlayerY(0);
            float restY = rest.y;
            int count = _player.Count;
            float[] raw = SecondDiffs(_player, false);
            float[] tc = SecondDiffs(_player, true);
            float[] camRaw = SecondDiffs(_cameraPos, false);
            float[] camTc = SecondDiffs(_cameraPos, true);
            float[] shipTc = SecondDiffs(_shipWorld, true);
            float[] rot = RotationJerks();
            bool[] steady = SteadyFrames(restY);
            float[] dt2 = new float[count];
            for (int i = 0; i < count; i++)
                dt2[i] = raw[i] / (_dts[i] * _dts[i]);

            Stat playerRaw = Stat.Of(raw, null);
            Stat playerTc = Stat.Of(tc, null);
            Stat playerDt2 = Stat.Of(dt2, null);
            Stat steadyRaw = Stat.Of(raw, steady);
            Stat steadyTc = Stat.Of(tc, steady);
            Stat cameraRaw = Stat.Of(camRaw, null);
            Stat cameraTc = Stat.Of(camTc, null);
            Stat cameraRot = Stat.Of(rot, null);
            Stat ship = Stat.Of(shipTc, null);
            float walk = PlanarSpeed(1);
            float sprint = PlanarSpeed(3);
            float apex = MaxY(4, 7) - restY;
            float air = AirTime(restY);
            float yawMin = float.MaxValue;
            float yawMax = float.MinValue;
            for (int i = 0; i < _cameraRot.Count; i++) {
                float yaw = Mathf.DeltaAngle(0f, _cameraRot[i].eulerAngles.y);
                yawMin = Mathf.Min(yawMin, yaw);
                yawMax = Mathf.Max(yawMax, yaw);
            }

            float slowest = 0f;
            float dtStep = 0f;
            for (int i = 1; i < count; i++) {
                slowest = Mathf.Max(slowest, _dts[i]);
                dtStep = Mathf.Max(dtStep, Mathf.Abs(_dts[i] - _dts[i - 1]));
            }

            string kind = label.EndsWith("fp") ? "fp" : "tp";
            string traceLine = "";
            if (label.StartsWith("dock") && _client == false)
                _dockTraces[kind] = new List<Vector3>(_player);
            else if (_fixedDt && _dockTraces.TryGetValue(kind, out List<Vector3> dock))
                traceLine = TraceVsDock(dock);

            float dockRms = 0f;
            if (_dockTraces.ContainsKey(kind) && label.StartsWith("dock") == false)
                dockRms = DockRms(kind);

            Note(
                label + " [" + Role() + ", " + _mode + "] samples=" + count
                + " slowestDt=" + slowest.ToString("0.000") + " maxDtStep=" + dtStep.ToString("0.000")
                + "\n  player raw rms/max=" + playerRaw.Text("0.0000")
                + "  tc rms/max=" + playerTc.Text("0.0000")
                + "  dt2 rms/max=" + playerDt2.Text("0.0")
                + "\n  player steady raw max=" + steadyRaw.Max.ToString("0.0000")
                + "  steady tc max=" + steadyTc.Max.ToString("0.0000")
                + "  poseDriftMax=" + MaxPoseDrift().ToString("0.0000")
                + "\n  camera raw rms/max=" + cameraRaw.Text("0.0000")
                + "  tc rms/max=" + cameraTc.Text("0.0000")
                + "  rot deg rms/max=" + cameraRot.Text("0.0000")
                + "  shipLocalYaw range=" + (yawMax - yawMin).ToString("0.000") + " deg"
                + "\n  ship world tc rms/max=" + ship.Text("0.0000")
                + "\n  walk=" + walk.ToString("0.00") + " sprint=" + sprint.ToString("0.00")
                + " jumpApex=" + apex.ToString("0.00") + " airTime=" + air.ToString("0.00")
                + (dockRms > 0f ? " rawRmsVsDock=" + (playerRaw.Rms / dockRms).ToString("0.00") + "x" : "")
                + traceLine);
            _table.Append("| ").Append(label).Append(" | ").Append(Role()).Append(" | ").Append(_mode)
                .Append(" | ").Append(playerRaw.Text("0.0000"))
                .Append(" | ").Append(playerTc.Text("0.0000"))
                .Append(" | ").Append(steadyRaw.Max.ToString("0.0000"))
                .Append(" | ").Append(cameraRaw.Text("0.0000"))
                .Append(" | ").Append(cameraRot.Max.ToString("0.000"))
                .Append(" | ").Append(ship.Text("0.0000"))
                .Append(" | ").Append(walk.ToString("0.00")).Append(" / ").Append(sprint.ToString("0.00"))
                .Append(" | ").Append(apex.ToString("0.00")).Append(" / ").Append(air.ToString("0.00"))
                .AppendLine(" |");
            NoteSpikes(raw, tc, steady);
            WriteFrames(label, raw, tc, dt2, camRaw, shipTc, steady);
            _player.Clear();
            _cameraPos.Clear();
            _cameraRot.Clear();
            _shipWorld.Clear();
            _steps.Clear();
            _dts.Clear();
            _events.Clear();
            _measureStep = 0;
            _measureTime = 0f;
            _warmup = 0;
            _measureReady = false;
        }

        private float DockRms(string kind) {
            List<Vector3> dock = _dockTraces[kind];
            float sum = 0f;
            int count = 0;
            for (int i = 2; i < dock.Count; i++) {
                float jerk = (dock[i] - 2f * dock[i - 1] + dock[i - 2]).magnitude;
                sum += jerk * jerk;
                count += 1;
            }

            return count > 0 ? Mathf.Sqrt(sum / count) : 0f;
        }

        // Fixed dt replays the identical input timeline, so a deck that is exactly static gives the same trace.
        private string TraceVsDock(List<Vector3> dock) {
            int count = Mathf.Min(dock.Count, _player.Count);
            if (count < 3)
                return "";

            Vector3 offset = _player[0] - dock[0];
            float posMax = 0f;
            float jerkMax = 0f;
            for (int i = 0; i < count; i++) {
                posMax = Mathf.Max(posMax, (_player[i] - dock[i] - offset).magnitude);
                if (i < 2)
                    continue;

                Vector3 cruise = _player[i] - 2f * _player[i - 1] + _player[i - 2];
                Vector3 docked = dock[i] - 2f * dock[i - 1] + dock[i - 2];
                jerkMax = Mathf.Max(jerkMax, (cruise - docked).magnitude);
            }

            return "\n  vs dock trace (same frames " + count + "/" + dock.Count + "): posDeltaMax="
                + posMax.ToString("0.00000") + " jerkDeltaMax=" + jerkMax.ToString("0.00000");
        }

        private float[] SecondDiffs(List<Vector3> samples, bool timeCorrect) {
            float[] result = new float[samples.Count];
            for (int i = 2; i < samples.Count; i++) {
                Vector3 step = samples[i] - samples[i - 1];
                Vector3 previous = samples[i - 1] - samples[i - 2];
                float scale = timeCorrect ? _dts[i] / _dts[i - 1] : 1f;
                result[i] = (step - previous * scale).magnitude;
            }

            return result;
        }

        private float[] RotationJerks() {
            float[] result = new float[_cameraRot.Count];
            for (int i = 2; i < _cameraRot.Count; i++) {
                Quaternion d0 = Quaternion.Inverse(_cameraRot[i - 2]) * _cameraRot[i - 1];
                Quaternion d1 = Quaternion.Inverse(_cameraRot[i - 1]) * _cameraRot[i];
                result[i] = Quaternion.Angle(d0, d1);
            }

            return result;
        }

        // Frames whose velocity step comes from the input itself: a stick / sprint change, or a jump takeoff,
        // flight or touchdown. The deck must not add anything on the other frames.
        private bool[] SteadyFrames(float restY) {
            int count = _player.Count;
            bool[] steady = new bool[count];
            for (int i = 2; i < count; i++) {
                bool inputStep = false;
                for (int k = Mathf.Max(1, i - 2); k <= i; k++) {
                    FrameEvents now = _events[k];
                    FrameEvents before = _events[k - 1];
                    if (now.Move != before.Move || now.Sprint != before.Sprint || now.Jump != before.Jump)
                        inputStep = true;
                }

                bool air = false;
                for (int k = i - 2; k <= i; k++) {
                    if (_player[k].y > restY + 0.002f)
                        air = true;
                }

                steady[i] = inputStep == false && air == false;
            }

            return steady;
        }

        private float MaxPoseDrift() {
            float max = 0f;
            for (int i = 0; i < _events.Count; i++)
                max = Mathf.Max(max, _events[i].PoseDrift);

            return max;
        }

        private void NoteSpikes(float[] raw, float[] tc, bool[] steady) {
            StringBuilder lines = new StringBuilder(1024);
            int written = 0;
            int total = 0;
            for (int i = 2; i < raw.Length; i++) {
                FrameEvents e = _events[i];
                bool spike = raw[i] > SpikeMeters || tc[i] > SpikeMeters;
                if (spike == false && e.Shifts == 0)
                    continue;

                total += 1;
                if (written >= 60)
                    continue;

                written += 1;
                lines.Append("  ").Append(spike ? "spike" : "shift").Append(" i=").Append(i)
                    .Append(" frame=").Append(e.Frame)
                    .Append(" dt=").Append(_dts[i].ToString("0.0000"))
                    .Append(" prevDt=").Append(_dts[i - 1].ToString("0.0000"))
                    .Append(" fixed=").Append(e.Fixed)
                    .Append(" shift=").Append(e.Shifts)
                    .Append(" gc=").Append(e.Gc)
                    .Append(" phase=").Append(e.Phase)
                    .Append(" step=").Append(_steps[i])
                    .Append(" raw=").Append(raw[i].ToString("0.0000"))
                    .Append(" tc=").Append(tc[i].ToString("0.0000"))
                    .Append(" cause=").Append(SpikeCause(i, raw[i], tc[i], steady[i]))
                    .AppendLine();
            }

            if (total == 0)
                return;

            Note("  events (spikes > " + SpikeMeters.ToString("0.00") + " m and shift frames): " + total);
            Note(lines.ToString().TrimEnd());
        }

        private string SpikeCause(int i, float raw, float tc, bool steady) {
            if (raw <= SpikeMeters && tc <= SpikeMeters)
                return "none";

            if (steady == false) {
                bool vertical = Mathf.Abs(_player[i].y - _player[i - 1].y) > 0.0001f
                    || Mathf.Abs(_player[i - 1].y - _player[i - 2].y) > 0.0001f;
                return vertical ? "jump-velocity-step" : "input-velocity-step";
            }

            if (tc <= SpikeMeters)
                return "frame-time-variance";

            return "UNEXPLAINED";
        }

        private void WriteFrames(string label, float[] raw, float[] tc, float[] dt2, float[] camRaw, float[] shipTc, bool[] steady) {
            StringBuilder csv = new StringBuilder(_player.Count * 160);
            csv.AppendLine("i,frame,dt,fixedSteps,shift,gc,phase,step,moveX,moveY,sprint,jump,steady,"
                + "px,py,pz,raw,tc,dt2,camRaw,shipTc,poseDrift,camYaw,camPitch,shipYaw");
            for (int i = 0; i < _player.Count; i++) {
                FrameEvents e = _events[i];
                Vector3 p = _player[i];
                csv.Append(i).Append(',').Append(e.Frame).Append(',').Append(F(_dts[i]))
                    .Append(',').Append(e.Fixed).Append(',').Append(e.Shifts)
                    .Append(',').Append(e.Gc)
                    .Append(',').Append(e.Phase).Append(',').Append(_steps[i])
                    .Append(',').Append(F(e.Move.x)).Append(',').Append(F(e.Move.y))
                    .Append(',').Append(e.Sprint ? 1 : 0).Append(',').Append(e.Jump ? 1 : 0)
                    .Append(',').Append(steady[i] ? 1 : 0)
                    .Append(',').Append(F(p.x)).Append(',').Append(F(p.y)).Append(',').Append(F(p.z))
                    .Append(',').Append(F(raw[i])).Append(',').Append(F(tc[i])).Append(',').Append(F(dt2[i]))
                    .Append(',').Append(F(camRaw[i])).Append(',').Append(F(shipTc[i]))
                    .Append(',').Append(F(e.PoseDrift))
                    .Append(',').Append(F(_cameraRot[i].eulerAngles.y)).Append(',').Append(F(_cameraRot[i].eulerAngles.x))
                    .Append(',').Append(F(e.ShipYaw))
                    .AppendLine();
            }

            string name = "frames-" + Role() + "-" + _mode + "-" + label + ".csv";
            File.WriteAllText(Path.Combine(_runDir, "frames", name), csv.ToString());
        }

        private static string F(float value) {
            return value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
        }

        private float PlanarSpeed(int step) {
            float distance = 0f;
            float time = 0f;
            for (int i = 1; i < _player.Count; i++) {
                if (_steps[i] != step || _steps[i - 1] != step)
                    continue;

                Vector3 delta = _player[i] - _player[i - 1];
                delta.y = 0f;
                distance += delta.magnitude;
                time += _dts[i];
            }

            return time > 0.0001f ? distance / time : 0f;
        }

        private float MaxY(int from, int to) {
            float max = float.MinValue;
            for (int i = 0; i < _player.Count; i++) {
                if (_steps[i] < from || _steps[i] > to)
                    continue;

                if (_player[i].y > max)
                    max = _player[i].y;
            }

            return max == float.MinValue ? 0f : max;
        }

        private Vector3 AveragePlayerY(int step) {
            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < _player.Count; i++) {
                if (_steps[i] != step)
                    continue;

                sum += _player[i];
                count += 1;
            }

            return count > 0 ? sum / count : Vector3.zero;
        }

        private float AirTime(float restY) {
            float time = 0f;
            for (int i = 0; i < _player.Count; i++) {
                if (_steps[i] < 4)
                    continue;

                if (_player[i].y > restY + 0.05f)
                    time += _dts[i];
            }

            return time;
        }

        // Client role (a player build joining the editor host): measures its own player riding the interpolated pose.
        private void TickClient() {
            if (_state == 0)
                TickJoin();
            else if (_state == 1)
                TickClientWaitActors();
            else if (_state == 20)
                TickMeasure(false, CameraIds.FPCamera, "client-dock-fp");
            else if (_state == 21)
                TickClientWaitCruise();
            else if (_state == 22)
                TickMeasure(true, CameraIds.FPCamera, "client-cruise-fp");
            else if (_state == 23)
                TickMeasure(true, CameraIds.TPCamera, "client-cruise-tp");
        }

        private void TickJoin() {
            Status = "join";
            if (NetworkClient.isConnected && NetworkClient.ready) {
                Enter(1);
                return;
            }

            if (Age() < 3f)
                return;

            if (_join == null) {
                _join = JoinAsync();
                return;
            }

            if (_join.IsFaulted) {
                _hostFailures += 1;
                Note("Join failed: " + _join.Exception.GetBaseException().Message);
                _join = null;
                _stateStart = Time.realtimeSinceStartup;
                if (_hostFailures >= 40)
                    Fail("could not join");
            }
            else if (Age() > 90f) {
                Fail("timed out joining");
            }
        }

        private async Task JoinAsync() {
            await _connection.JoinAsync("localhost");
            await _flow.EnterAsync<SessionGameFlowState>();
        }

        private void TickClientWaitActors() {
            Status = "client-wait-actors";
            if (_ship == null)
                _ship = UnityEngine.Object.FindFirstObjectByType<ShipBase>();

            if (_rider == null && _ship != null)
                _rider = FindOwnedRider();

            if (_ship == null || _rider == null || _model.Phase != ShipRunPhase.Build || Age() < 2f) {
                if (Age() > 240f)
                    Fail("client timed out waiting for the ship and its player");

                return;
            }

            if (_rider.IsRiding == false) {
                PlaceRiderOnDeck();
                _ship.Riders.DebugBindRider(_rider);
            }

            if (_rider.IsRiding == false)
                return;

            _input.BeginScripted();
            Note("Client riding in Build. players=" + UnityEngine.Object.FindObjectsByType<ShipRider>(FindObjectsSortMode.None).Length);
            Enter(20);
        }

        private void TickClientWaitCruise() {
            Status = "client-wait-cruise";
            _input.SetScripted(Vector2.zero, false, false);
            if (_clientLegs >= 2) {
                if (_pendingShot == null) {
                    Finish(true);
                    return;
                }
            }

            if (_model.Phase != ShipRunPhase.Cruise) {
                _stateStart = Time.realtimeSinceStartup;
                return;
            }

            if (_pendingShot != null) {
                _input.SetScripted(new Vector2(0f, 1f), false, false);
                if (Age() > 1f) {
                    Capture(_pendingShot);
                    _pendingShot = null;
                }

                return;
            }

            if (_rider.IsRiding == false) {
                Fail("client player is not riding in cruise");
                return;
            }

            if (Age() < 1.5f)
                return;

            _clientLegs += 1;
            if (_clientLegs == 1)
                _pendingShot = "client-cruise-fp";

            Enter(_clientLegs == 1 ? 22 : 23);
        }

        private string Role() {
            return _client ? "client" : "host";
        }

        private void InstallRequiredModules() {
            ShipSocket[] sockets = _ship.Sockets;
            if (sockets == null)
                return;

            for (int i = 0; i < sockets.Length; i++) {
                ShipSocket socket = sockets[i];
                if (socket == null || socket.RequiredForLaunch == false || socket.IsOccupied)
                    continue;

                if (TryFindDrop(socket.AcceptedType, out ShipItem item) == false)
                    continue;

                socket.ServerTryInstall(item.Type, item.View);
            }
        }

        private bool TryFindDrop(ShipModuleType type, out ShipItem item) {
            item = null;
            if (_stations.Drops == null)
                return false;

            for (int i = 0; i < _stations.Drops.Length; i++) {
                GameObject prefab = _stations.Drops[i].Prefab;
                if (prefab == null)
                    continue;

                ShipItem candidate = prefab.GetComponent<ShipItem>();
                if (candidate == null || candidate.Type != type)
                    continue;

                item = candidate;
                return true;
            }

            return false;
        }

        private void PlaceRiderOnDeck() {
            if (_ship.DeckGeometry.TryGetDeckStandPoint(out Vector3 local) == false) {
                _rider.transform.position = _ship.transform.position + Vector3.up;
                return;
            }

            _rider.transform.position = _ship.transform.TransformPoint(local);
        }

        private void StandOnDeck() {
            if (_rider.IsRiding == false) {
                PlaceRiderOnDeck();
                _ship.Riders.DebugBindRider(_rider);
                return;
            }

            if (_ship.DeckGeometry.TryGetDeckStandPoint(out Vector3 local) == false)
                return;

            _rider.DebugSetLocalOffset(local);
        }

        private void SpawnDeckItem() {
            if (_itemSpawned || TryFindDrop(ShipModuleType.Engine, out ShipItem item) == false)
                return;

            Vector3 local = Vector3.up;
            if (_ship.DeckGeometry.TryGetDeckStandPoint(out Vector3 stand)) {
                local = stand;
                local.x += 1.4f;
                local.y -= 0.7f;
                if (_ship.DeckGeometry.ContainsDeckWalk(local, 0f) == false)
                    local = stand;
            }

            Vector3 point = _ship.transform.TransformPoint(local);
            GameObject spawned = UnityEngine.Object.Instantiate(item.gameObject, point, _ship.transform.rotation);
            NetworkServer.Spawn(spawned);
            _looseItem = spawned.GetComponent<Grabbable>();
            _itemSpawned = true;
        }

        private float CargoDrift() {
            if (_ship.Cargo.TryMeasure(_ship.transform, out Vector3 local, out float drift))
                _cargoDriftMax = Mathf.Max(_cargoDriftMax, drift);

            return _cargoDriftMax;
        }

        private void TryCaptureStation() {
            if (_model.Phase != ShipRunPhase.Cruise)
                return;

            Vector3 to = _model.TransitDestination - _ship.transform.position;
            to.y = 0f;
            float distance = to.magnitude;
            if (distance > 220f || distance < 45f)
                return;

            _ship.FlightControl.DebugFace(to);
            CameraLookDriver.DebugPitchOverride = true;
            CameraLookDriver.DebugPitch = 6f;
            if (_stationReady == false) {
                _stationReady = true;
                return;
            }

            Capture("cruise-station-ahead");
            _shotStation = true;
            CameraLookDriver.DebugPitchOverride = false;
        }

        private void Capture(string name) {
            Camera camera = _cameras.OutputCamera;
            if (camera == null)
                return;

            RenderTexture texture = RenderTexture.GetTemporary(1280, 720, 24);
            RenderTexture previous = camera.targetTexture;
            camera.targetTexture = texture;
            camera.Render();
            camera.targetTexture = previous;
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = texture;
            Texture2D image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
            image.Apply();
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(texture);
            File.WriteAllBytes(Path.Combine(_runDir, "screenshots", name + ".png"), image.EncodeToPNG());
            UnityEngine.Object.Destroy(image);
        }

        private string ModuleLine() {
            ShipSocket[] sockets = _ship.Sockets;
            if (sockets == null)
                return "no sockets";

            int occupied = 0;
            int required = 0;
            for (int i = 0; i < sockets.Length; i++) {
                ShipSocket socket = sockets[i];
                if (socket == null || socket.RequiredForLaunch == false)
                    continue;

                required += 1;
                if (socket.IsOccupied)
                    occupied += 1;
            }

            return "required " + occupied + "/" + required + " flightMode=" + _ship.FlightControl.Mode;
        }

        private static ShipRider FindOwnedRider() {
            ShipRider[] riders = UnityEngine.Object.FindObjectsByType<ShipRider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < riders.Length; i++) {
                if (riders[i] != null && riders[i].isOwned)
                    return riders[i];
            }

            return null;
        }

        private void Enter(int state) {
            _state = state;
            _stateStart = Time.realtimeSinceStartup;
            _measureStep = 0;
            _measureTime = 0f;
            _warmup = 0;
            _measureReady = false;
            _approachReady = false;
            _stationReady = false;
            if (state == 5 || state == 10)
                _cargoNoted = false;
        }

        private float Age() {
            return Time.realtimeSinceStartup - _stateStart;
        }

        private void Finish(bool success) {
            if (success && _client == false && _ship != null && _ship.Cargo.AttachedCount == 0) {
                Note("no loose item stayed on the deck");
                success = false;
            }

            if (_table.Length > 0) {
                Note("| case | role | dt | player raw rms / max | player tc rms / max | steady raw max | camera raw rms / max | cam rot max deg | ship world tc rms / max | walk / sprint | apex / air |");
                Note("|---|---|---|---|---|---|---|---|---|---|---|");
                Note(_table.ToString().TrimEnd());
            }

            if (_fixedDt)
                Time.captureFramerate = 0;

            Note("worldShifts=" + _worldShift.WorldShiftCount + " loops=" + _model.LoopIndex + " cargoDriftMax=" + _cargoDriftMax.ToString("0.000") + " cargo=" + (_ship != null ? _ship.Cargo.AttachedCount : 0));
            Note(success ? "PASS" : "FAIL");
            Done = success ? 1 : -1;
            Status = success ? "done" : "failed";
            _input.EndScripted();
            if (_ship != null)
                _ship.FlightControl.SetDebugSteer(false, 0f);

            _stopped = true;
            WriteReport();
        }

        private void Fail(string reason) {
            Note("FAIL " + reason);
            Finish(false);
        }

        private void Note(string line) {
            _report.AppendLine(line);
            WriteReport();
        }

        private void WriteReport() {
            string name = "deck-feel-" + Role() + "-" + _mode + ".txt";
            File.WriteAllText(Path.Combine(_runDir, name), _report.ToString());
        }

        // A "dir=<folder>" line in the request writes the output to that run folder instead of the default one.
        private static string ReadRunDir(string request) {
            foreach (string line in request.Split('\n')) {
                string trimmed = line.Trim();
                if (trimmed.StartsWith(RunDirPrefix, StringComparison.Ordinal))
                    return trimmed.Substring(RunDirPrefix.Length);
            }

            return DefaultRunDir;
        }

        private static string RequestPath() {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "deck-feel.request"));
        }

        private readonly struct FrameEvents {
            public FrameEvents(int frame, int fixedSteps, int shifts, int gc, ShipRunPhase phase,
                Vector2 move, bool sprint, bool jump, float poseDrift, float shipYaw) {
                Frame = frame;
                Fixed = fixedSteps;
                Shifts = shifts;
                Gc = gc;
                Phase = phase;
                Move = move;
                Sprint = sprint;
                Jump = jump;
                PoseDrift = poseDrift;
                ShipYaw = shipYaw;
            }

            public int Frame { get; }
            public int Fixed { get; }
            public int Shifts { get; }
            public int Gc { get; }
            public ShipRunPhase Phase { get; }
            public Vector2 Move { get; }
            public bool Sprint { get; }
            public bool Jump { get; }
            public float PoseDrift { get; }
            public float ShipYaw { get; }
        }

        private readonly struct Stat {
            private Stat(float rms, float max) {
                Rms = rms;
                Max = max;
            }

            public float Rms { get; }
            public float Max { get; }

            public static Stat Of(float[] values, bool[] mask) {
                float sum = 0f;
                float max = 0f;
                int count = 0;
                for (int i = 2; i < values.Length; i++) {
                    if (mask != null && mask[i] == false)
                        continue;

                    sum += values[i] * values[i];
                    max = Mathf.Max(max, values[i]);
                    count += 1;
                }

                return new Stat(count > 0 ? Mathf.Sqrt(sum / count) : 0f, max);
            }

            public string Text(string format) {
                return Rms.ToString(format) + " / " + Max.ToString(format);
            }
        }
    }
}
#endif
