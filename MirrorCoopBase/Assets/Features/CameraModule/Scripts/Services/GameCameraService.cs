using System;
using Features.CameraModule.Scripts.Models;
using Features.GameCoreModule.Scripts;
using Features.GrabModule.Scripts;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using Zenject;

namespace Features.CameraModule.Scripts.Services {
    public sealed class GameCameraService : IGameCameraService, IInitializable, IDisposable, IGameplaySession {
        private const int LivePriority = 100;
        private const int StandbyPriority = 10;
        private const string ORBIT_HOLD_ANCHOR_NAME = "OrbitHoldAnchor";

        private readonly GameCameraModel _model;
        private readonly CameraCatalog _catalog;

        private Transform _root;
        private Camera _output;
        private CinemachineBrain _brain;
        private CameraLookRig _lookRig;
        internal static Action AfterPresent;

        private static GameCameraService _presenting;

        public GameCameraService(GameCameraModel model, CameraCatalog catalog) {
            _model = model;
            _catalog = catalog;
        }

        public string ActiveId => _model.ActiveId;

        public Camera OutputCamera {
            get {
                EnsureOutputCamera();
                return _output;
            }
        }

        public void Initialize() {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _presenting = this;
            InstallPresentHook();
            SpawnIdleCameras();
        }

        public void Dispose() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (_presenting == this)
                _presenting = null;

            TearDownCameras();
        }

        public void CleanupGameplay() {
            TearDownCameras();
            SpawnIdleCameras();
        }

        public void RestartGameplay() {
            Transform follow = _model.Follow;
            Transform lookAt = _model.LookAt;
            Transform eye = _model.Eye;
            string activeId = _model.ActiveId;
            CleanupGameplay();
            if (follow != null)
                BindToLocalPlayer(follow, lookAt, eye);
            if (string.IsNullOrEmpty(activeId) == false)
                BlendTo(activeId, 0f);
        }

        public void Register(GameCamera camera) {
            _model.Register(camera);
            ApplyBinding(camera);
        }

        public void Unregister(GameCamera camera) {
            _model.Unregister(camera);
        }

        public bool TryGet(string id, out GameCamera camera) {
            return _model.TryGet(id, out camera);
        }

        public void BindToLocalPlayer(Transform follow, Transform lookAt, Transform eye) {
            EnsureRoot();
            EnsureLookPivot();

            _model.Follow = follow;
            _model.LookAt = lookAt;
            _model.Eye = eye;

            foreach (GameCamera camera in _model.Cameras.Values)
                ApplyBinding(camera);

            if (string.IsNullOrEmpty(_model.ActiveId))
                BlendTo(_catalog != null ? _catalog.StartupCameraId : CameraIds.TPCamera, 0f);
        }

        // The look pivot is parented to the current target (CameraLookRig): detach it first, or a target that leaves takes
        // the pivot down with it and the cameras fall back to a new pivot at the world origin.
        public void RetargetOrbit(Transform follow) {
            ReturnLookPivotToRoot();
            _model.Follow = follow;
            _model.LookAt = follow;
            _model.Eye = follow;
            foreach (GameCamera camera in _model.Cameras.Values)
                ApplyBinding(camera);
        }

        public void HoldOrbit() {
            EnsureRoot();
            EnsureLookPivot();
            if (_model.OrbitHoldAnchor == null) {
                GameObject anchorObject = new GameObject(ORBIT_HOLD_ANCHOR_NAME);
                anchorObject.transform.SetParent(_root, false);
                _model.OrbitHoldAnchor = anchorObject.transform;
            }

            Transform anchor = _model.OrbitHoldAnchor;
            Transform pivot = _model.LookPivot;
            anchor.SetPositionAndRotation(pivot.position, pivot.rotation);
            RetargetOrbit(anchor);
        }

        public void ClearLocalPlayer() {
            ReturnLookPivotToRoot();
            _model.Follow = null;
            _model.LookAt = null;
            _model.Eye = null;

            foreach (GameCamera camera in _model.Cameras.Values)
                ApplyBinding(camera);
        }

        public void BlendTo(string id, float duration = -1f) {
            if (_model.TryGet(id, out GameCamera next) == false || next == null)
                return;

            EnsureBrain();
            float blendTime = duration >= 0f
                ? duration
                : _catalog != null ? _catalog.DefaultBlendSeconds : 0.45f;
            if (_brain != null) {
                _brain.DefaultBlend = blendTime <= 0.001f
                    ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f)
                    : new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendTime);
            }

            foreach (GameCamera camera in _model.Cameras.Values)
                camera.SetPriority(camera == next ? LivePriority : StandbyPriority);

            _model.ActiveId = id;
        }

        public void ToggleFpAndTp() {
            string next = ActiveId == CameraIds.FPCamera ? CameraIds.TPCamera : CameraIds.FPCamera;
            BlendTo(next);
        }

        private void SpawnIdleCameras() {
            EnsureRoot();
            EnsureOutputCamera();
            EnsureBrain();
            DisableLoadedSceneCameras();
            SpawnCameras();
        }

        // The look pivot rides on the player (CameraLookRig): bring it back under the root so it goes down with it instead
        // of staying on the player next to the pivot the rebuilt cameras create.
        private void TearDownCameras() {
            ReturnLookPivotToRoot();
            if (_root != null)
                UnityEngine.Object.Destroy(_root.gameObject);

            _root = null;
            _output = null;
            _brain = null;
            _model.Clear();
        }

        private void SpawnCameras() {
            GameCamera fpPrefab = _catalog != null ? _catalog.FpCameraPrefab : null;
            GameCamera tpPrefab = _catalog != null ? _catalog.TpCameraPrefab : null;
            Spawn(CameraIds.FPCamera, GameCameraKind.FirstPerson, fpPrefab);
            Spawn(CameraIds.TPCamera, GameCameraKind.ThirdPerson, tpPrefab);
        }

        private void Spawn(string id, GameCameraKind kind, GameCamera prefab) {
            if (_model.TryGet(id, out GameCamera existing) && existing != null)
                return;

            GameCamera camera = prefab != null
                ? UnityEngine.Object.Instantiate(prefab, _root)
                : GameCamera.Create(id, kind, _root);

            camera.name = id;
            Register(camera);
        }

        private void ApplyBinding(GameCamera camera) {
            if (camera == null)
                return;

            Transform target = _model.LookPivot != null ? _model.LookPivot : _model.Follow;
            camera.Bind(target, null);
            camera.LockFollowToTarget();
        }

        private void EnsureRoot() {
            if (_root != null)
                return;

            var rootObject = new GameObject("GameCameras");
            UnityEngine.Object.DontDestroyOnLoad(rootObject);
            _root = rootObject.transform;
            EnsureLookPivot();
        }

        private void EnsureLookPivot() {
            if (_model.LookPivot != null)
                return;

            var pivotObject = new GameObject("CameraLook");
            pivotObject.transform.SetParent(_root, false);
            pivotObject.AddComponent<CameraLookRig>();
            _model.LookPivot = pivotObject.transform;
        }

        private void ReturnLookPivotToRoot() {
            Transform pivot = _model.LookPivot;
            if (pivot == null || _root == null)
                return;

            if (pivot.parent != _root)
                pivot.SetParent(_root, true);
        }

        private void EnsureOutputCamera() {
            if (_output != null)
                return;

            EnsureRoot();
            var outputObject = new GameObject("GameCameraOutput");
            outputObject.tag = "MainCamera";
            outputObject.transform.SetParent(_root, false);
            _output = outputObject.AddComponent<Camera>();
            _output.nearClipPlane = 0.1f;
            if (outputObject.GetComponent<AudioListener>() == null)
                outputObject.AddComponent<AudioListener>();
        }

        private void EnsureBrain() {
            EnsureOutputCamera();
            if (_output == null)
                return;

            if (_brain != null && _brain.gameObject == _output.gameObject)
                return;

            _brain = _output.GetComponent<CinemachineBrain>();
            if (_brain == null)
                _brain = _output.gameObject.AddComponent<CinemachineBrain>();

            _brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            _brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            DisableSceneCameras(scene);
            EnsureBrain();
        }

        private void DisableLoadedSceneCameras() {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                DisableSceneCameras(SceneManager.GetSceneAt(i));
        }

        private void DisableSceneCameras(Scene scene) {
            if (scene.IsValid() == false || scene.isLoaded == false)
                return;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++) {
                Camera[] cameras = roots[i].GetComponentsInChildren<Camera>(true);
                for (int j = 0; j < cameras.Length; j++)
                    DisableSceneCamera(cameras[j]);
            }
        }

        private void DisableSceneCamera(Camera camera) {
            if (camera == null || camera == _output)
                return;

            if (_root != null && camera.transform.IsChildOf(_root))
                return;

            camera.enabled = false;
            if (camera.CompareTag("MainCamera"))
                camera.tag = "Untagged";

            AudioListener listener = camera.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = false;
        }

        private static void InstallPresentHook() {
            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
            if (Contains(loop, typeof(GameCameraService)))
                return;

            if (TryInsertPresent(ref loop) == false)
                return;

            PlayerLoop.SetPlayerLoop(loop);
        }

        private static bool TryInsertPresent(ref PlayerLoopSystem loop) {
            PlayerLoopSystem[] systems = loop.subSystemList;
            if (systems == null)
                return false;

            for (int i = 0; i < systems.Length; i++) {
                if (systems[i].type != typeof(PostLateUpdate))
                    continue;

                PlayerLoopSystem[] sub = systems[i].subSystemList;
                int count = sub != null ? sub.Length : 0;
                PlayerLoopSystem[] next = new PlayerLoopSystem[count + 1];
                next[0] = new PlayerLoopSystem {
                    type = typeof(GameCameraService),
                    updateDelegate = PresentAfterSimulation
                };
                for (int n = 0; n < count; n++)
                    next[n + 1] = sub[n];

                systems[i].subSystemList = next;
                loop.subSystemList = systems;
                return true;
            }

            return false;
        }

        private static bool Contains(PlayerLoopSystem system, Type type) {
            if (system.type == type)
                return true;

            PlayerLoopSystem[] sub = system.subSystemList;
            if (sub == null)
                return false;

            for (int i = 0; i < sub.Length; i++) {
                if (Contains(sub[i], type))
                    return true;
            }

            return false;
        }

        private static void PresentAfterSimulation() {
            if (_presenting != null)
                _presenting.PresentNow();
        }

        private void PresentNow() {
            ApplyLookRig();
            Grabbable.FollowHeldAll();
            EnsureBrain();
            if (_brain != null)
                _brain.ManualUpdate();

            Action after = AfterPresent;
            if (after != null)
                after();
        }

        private void ApplyLookRig() {
            Transform pivot = _model.LookPivot;
            if (pivot == null)
                return;

            if (_lookRig == null || _lookRig.transform != pivot)
                _lookRig = pivot.GetComponent<CameraLookRig>();

            if (_lookRig != null)
                _lookRig.Apply();
        }
    }
}
