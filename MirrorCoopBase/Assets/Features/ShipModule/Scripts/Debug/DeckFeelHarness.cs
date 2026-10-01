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
        private const string RunDir = @"C:\Users\User\.agent-orchestrator\runs\20261001-2145-flight-in-space";
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
        private readonly ShipRunService _run;
        private readonly ShipRunModel _model;
        private readonly ShipStationCatalog _stations;

        private readonly List<Vector3> _player = new List<Vector3>(900);
        private readonly List<Vector3> _cameraPos = new List<Vector3>(900);
        private readonly List<Quaternion> _cameraRot = new List<Quaternion>(900);
        private readonly List<int> _steps = new List<int>(900);
        private readonly List<float> _dts = new List<float>(900);
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
        private int _flightPass;
        private int _loopsSeen;
        private bool _shotStation;
        private bool _shotRock;
        private bool _shotApproach;
        private Vector3 _hoverStart;
        private bool _hoverSampled;
        private float _cargoDriftMax;

        public DeckFeelHarness(
            CharacterInputBuffer input,
            IGameCameraService cameras,
            IConnectionSessionService connection,
            IGameFlowStateMachineService flow,
            ShipRunService run,
            ShipRunModel model,
            ShipStationCatalog stations) {
            _input = input;
            _cameras = cameras;
            _connection = connection;
            _flow = flow;
            _run = run;
            _model = model;
            _stations = stations;
        }

        public void Tick() {
            if (_stopped)
                return;

            if (_gateChecked == false) {
                _gateChecked = true;
                if (File.Exists(RequestPath()) == false) {
                    _stopped = true;
                    return;
                }

                Directory.CreateDirectory(Path.Combine(RunDir, "screenshots"));
                Status = "armed";
                Note("Deck feel harness armed.");
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

        private async Task HostAsync() {
            await _connection.HostAsync();
            await _flow.EnterAsync<SessionGameFlowState>();
        }

        private void TickWaitActors() {
            Status = "wait-actors";
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
                _ship.DebugBindRider(_rider);
                if (_rider.IsRiding == false) {
                    Vector3 local = _ship.transform.InverseTransformPoint(_rider.transform.position);
                    Fail("local player did not bind to the deck at " + local.ToString("0.00"));
                    return;
                }
            }

            _input.BeginScripted();
            _cameras.BlendTo(CameraIds.FPCamera, 0f);
            Note("Modules: " + ModuleLine());
            Enter(3);
        }

        private void TickLaunch(bool hover) {
            Status = hover ? "launch-hover" : "launch-travel";
            _input.EndScripted();
            _ship.SetDebugSteer(false, 0f);
            if (_ship.CanLaunch == false) {
                InstallRequiredModules();
                if (_ship.CanLaunch == false) {
                    Fail("ship cannot launch. " + ModuleLine());
                    return;
                }
            }

            if (_cargoNoted == false) {
                _cargoNoted = true;
                if (_ship.DeckCargoCount == 0 && _looseItem != null) {
                    _ship.DebugAttachDeckItem(_looseItem);
                    Note("Deck scan missed the loose item; pinned it directly. cargo=" + _ship.DeckCargoCount);
                }
                else {
                    Note("Deck cargo attached by scan. cargo=" + _ship.DeckCargoCount);
                }
            }

            if (hover)
                _run.DebugUseFlightMode(ShipFlightMode.HoverInPlace);
            else
                _run.DebugClearFlightMode();

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

            if (_shotRock == false)
                TryCaptureRock();

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

                    CameraLookDriver.DebugPitchOverride = cruise && cameraId == CameraIds.FPCamera;
                    CameraLookDriver.DebugPitch = 28f;
                    _warmup = 8;
                }

                _warmup -= 1;
                _input.SetScripted(Vector2.zero, false, false);
                if (_warmup > 0)
                    return;

                _measureReady = true;
            }

            if (cruise) {
                _ship.SetDebugSteer(true, Mathf.Sin(Time.time * 1.7f) * 0.55f);
                TryCaptureRock();
                if (_model.Phase == ShipRunPhase.Landing && _shotApproach == false) {
                    Capture("approach-pad");
                    _shotApproach = true;
                }
            }

            bool walking = _measureStep == 1 || _measureStep == 2 || _measureStep == 3;
            if (cruise && walking && _measureTime > 0.4f && _measureTime < 0.55f)
                Capture(cameraId == CameraIds.FPCamera ? "cruise-walk-fp" : "cruise-walk-tp");

            ApplyMeasureInput();
            RecordSample();
            if (AdvanceMeasureClock() == false)
                return;

            AppendMetrics(label);
            _ship.SetDebugSteer(false, 0f);
            Enter(_state == 7 ? 9 : _state + 1);
        }

        private void TickFinishLeg(bool hoverDone) {
            Status = hoverDone ? "hover-land" : "travel-land";
            _ship.SetDebugSteer(false, 0f);
            if (_model.Phase == ShipRunPhase.Cruise || _model.Phase == ShipRunPhase.Takeoff) {
                Vector3 to = _model.TransitDestination - _ship.transform.position;
                _ship.DebugFace(to);
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
            Note("Landed loop " + _loopsSeen + ". " + ModuleLine() + " cargoDrift=" + CargoDrift().ToString("0.000") + " cargo=" + _ship.DeckCargoCount);
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
            Note("Hover cruise horizontal move in 3s: " + delta.magnitude.ToString("0.00") + " m (mode " + _ship.ActiveFlightMode + ").");
            if (delta.magnitude > 15f) {
                Fail("hover cruise translated " + delta.magnitude.ToString("0.00") + " m");
                return;
            }

            _run.DebugClearFlightMode();
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

            _steps.Add(_measureStep);
            _dts.Add(Mathf.Max(Time.deltaTime, 0.0001f));
        }

        private void AppendMetrics(string label) {
            Vector3 rest = AveragePlayerY(0);
            float restY = rest.y;
            SecondDiff player = Diff(_player);
            SecondDiff camera = Diff(_cameraPos);
            float rotRms = 0f;
            float rotMax = 0f;
            int rotCount = 0;
            for (int i = 2; i < _cameraRot.Count; i++) {
                if (_dts[i] > 0.05f || _dts[i - 1] > 0.05f)
                    continue;
                Quaternion d0 = Quaternion.Inverse(_cameraRot[i - 2]) * _cameraRot[i - 1];
                Quaternion d1 = Quaternion.Inverse(_cameraRot[i - 1]) * _cameraRot[i];
                float jerk = Quaternion.Angle(d0, d1);
                rotRms += jerk * jerk;
                if (jerk > rotMax)
                    rotMax = jerk;

                rotCount += 1;
            }

            float walk = PlanarSpeed(1);
            float sprint = PlanarSpeed(3);
            float apex = MaxY(4, 7) - restY;
            float air = AirTime(restY);
            Note(
                label
                + " playerJerkRms=" + player.Rms.ToString("0.000000")
                + " playerJerkMax=" + player.Max.ToString("0.000000")
                + " camPosJerkRms=" + camera.Rms.ToString("0.000000")
                + " camPosJerkMax=" + camera.Max.ToString("0.000000")
                + " camRotJerkRms=" + (rotCount > 0 ? Mathf.Sqrt(rotRms / rotCount) : 0f).ToString("0.0000")
                + " camRotJerkMax=" + rotMax.ToString("0.0000")
                + " walkMps=" + walk.ToString("0.00")
                + " sprintMps=" + sprint.ToString("0.00")
                + " jumpApex=" + apex.ToString("0.00")
                + " airTime=" + air.ToString("0.00")
                + " samples=" + _player.Count);
            _player.Clear();
            _cameraPos.Clear();
            _cameraRot.Clear();
            _steps.Clear();
            _dts.Clear();
            _measureStep = 0;
            _measureTime = 0f;
            _warmup = 0;
            _measureReady = false;
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

        private SecondDiff Diff(List<Vector3> samples) {
            float sum = 0f;
            float max = 0f;
            int count = 0;
            for (int i = 2; i < samples.Count; i++) {
                if (_dts[i] > 0.05f || _dts[i - 1] > 0.05f)
                    continue;

                Vector3 second = samples[i] - 2f * samples[i - 1] + samples[i - 2];
                float magnitude = second.magnitude;
                sum += magnitude * magnitude;
                if (magnitude > max)
                    max = magnitude;

                count += 1;
            }

            return new SecondDiff(count > 0 ? Mathf.Sqrt(sum / count) : 0f, max);
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
            if (_ship.TryGetDeckStandPoint(out Vector3 local) == false) {
                _rider.transform.position = _ship.transform.position + Vector3.up;
                return;
            }

            _rider.transform.position = _ship.transform.TransformPoint(local);
        }

        private void StandOnDeck() {
            if (_rider.IsRiding == false) {
                PlaceRiderOnDeck();
                _ship.DebugBindRider(_rider);
                return;
            }

            if (_ship.TryGetDeckStandPoint(out Vector3 local) == false)
                return;

            _rider.DebugSetLocalOffset(local);
        }

        private void SpawnDeckItem() {
            if (_itemSpawned || TryFindDrop(ShipModuleType.Engine, out ShipItem item) == false)
                return;

            Vector3 local = Vector3.up;
            if (_ship.TryGetDeckStandPoint(out Vector3 stand)) {
                local = stand;
                local.x += 1.4f;
                local.y -= 0.7f;
                if (_ship.ContainsDeckWalk(local, 0f) == false)
                    local = stand;
            }

            Vector3 point = _ship.transform.TransformPoint(local);
            GameObject spawned = UnityEngine.Object.Instantiate(item.gameObject, point, _ship.transform.rotation);
            NetworkServer.Spawn(spawned);
            _looseItem = spawned.GetComponent<Grabbable>();
            _itemSpawned = true;
        }

        private float CargoDrift() {
            if (_ship.TryMeasureDeckCargo(out Vector3 local, out float drift))
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

            _ship.DebugFace(to);
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

        private void TryCaptureRock() {
            if (_shotRock || _ship == null)
                return;

            CruiseRock[] rocks = UnityEngine.Object.FindObjectsByType<CruiseRock>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < rocks.Length; i++) {
                if (rocks[i] == null)
                    continue;

                if ((rocks[i].transform.position - _ship.transform.position).sqrMagnitude > 40f * 40f)
                    continue;

                Capture("passing-rocks");
                _shotRock = true;
                return;
            }
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
            File.WriteAllBytes(Path.Combine(RunDir, "screenshots", name + ".png"), image.EncodeToPNG());
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

            return "required " + occupied + "/" + required + " flightMode=" + _ship.ActiveFlightMode;
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
            if (success && _ship != null && _ship.DeckCargoCount == 0) {
                Note("no loose item stayed on the deck");
                success = false;
            }

            Note("worldShifts=" + _run.WorldShiftCount + " loops=" + _model.LoopIndex + " cargoDriftMax=" + _cargoDriftMax.ToString("0.000") + " cargo=" + (_ship != null ? _ship.DeckCargoCount : 0));
            Note(success ? "PASS" : "FAIL");
            Done = success ? 1 : -1;
            Status = success ? "done" : "failed";
            _input.EndScripted();
            if (_ship != null)
                _ship.SetDebugSteer(false, 0f);

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
            File.WriteAllText(Path.Combine(RunDir, "deck-feel-report.txt"), _report.ToString());
        }

        private static string RequestPath() {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "deck-feel.request"));
        }

        private readonly struct SecondDiff {
            public SecondDiff(float rms, float max) {
                Rms = rms;
                Max = max;
            }

            public float Rms { get; }
            public float Max { get; }
        }
    }
}
