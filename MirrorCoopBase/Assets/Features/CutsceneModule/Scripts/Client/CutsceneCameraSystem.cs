using System;
using System.Collections.Generic;
using Features.CameraModule.Scripts.Services;
using Features.CutsceneModule.Contracts;
using Features.CutsceneModule.Scripts.Configuration;
using Features.CutsceneModule.Scripts.Core;
using Features.CutsceneModule.Scripts.Models;
using Features.CutsceneModule.Scripts.Network;
using Features.CutsceneModule.Scripts.Network.Generated;
using Features.GameCoreModule.Contracts;
using Mirror;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Features.CutsceneModule.Scripts.Client {
    // Every peer: plays the synced cutscene on its own camera at the playback's network time. Its camera takes the
    // view from where the player looked (a cut, it starts at that pose), moves along the shots every frame before the
    // camera brain updates, and blends back to the player's camera when the shots end or someone skips.
    public sealed class CutsceneCameraSystem : IInitializable, IDisposable, IGameplaySession {
        private const string CAMERA_NAME = "CutsceneCamera";
        private const float CUT_SECONDS = 0f;

        private readonly IGameCameraService _gameCameraService;
        private readonly IReadOnlyCutsceneModel _cutsceneModel;
        private readonly CutsceneLocalModel _cutsceneLocalModel;
        private readonly ICutscenePlaybackRule _cutscenePlaybackRule;
        private readonly ICutsceneCameraPath _cutsceneCameraPath;
        private readonly List<ICutsceneTargetSource> _cutsceneTargetSources;
        private readonly CutsceneConfiguration _cutsceneConfiguration;
        private readonly List<Vector3> _focuses = new();

        private CinemachineCamera _camera;

        public CutsceneCameraSystem(
            IGameCameraService gameCameraService,
            IReadOnlyCutsceneModel cutsceneModel,
            CutsceneLocalModel cutsceneLocalModel,
            ICutscenePlaybackRule cutscenePlaybackRule,
            ICutsceneCameraPath cutsceneCameraPath,
            List<ICutsceneTargetSource> cutsceneTargetSources,
            CutsceneConfiguration cutsceneConfiguration) {
            _gameCameraService = gameCameraService;
            _cutsceneModel = cutsceneModel;
            _cutsceneLocalModel = cutsceneLocalModel;
            _cutscenePlaybackRule = cutscenePlaybackRule;
            _cutsceneCameraPath = cutsceneCameraPath;
            _cutsceneTargetSources = cutsceneTargetSources;
            _cutsceneConfiguration = cutsceneConfiguration;
        }

        public void Initialize() {
            _camera = CreateCamera();
            _gameCameraService.LookApplied += OnLookApplied;
        }

        public void Dispose() {
            _gameCameraService.LookApplied -= OnLookApplied;
            _cutsceneLocalModel.Forget();
            Object.Destroy(_camera.gameObject);
        }

        public void CleanupGameplay() =>
            ForgetPlayback();

        public void RestartGameplay() =>
            ForgetPlayback();

        private CinemachineCamera CreateCamera() {
            GameObject cameraObject = new(CAMERA_NAME);
            Object.DontDestroyOnLoad(cameraObject);
            CinemachineCamera camera = cameraObject.AddComponent<CinemachineCamera>();
            LensSettings lens = camera.Lens;
            lens.FieldOfView = _cutsceneConfiguration.FieldOfView;
            camera.Lens = lens;
            camera.Priority = 0;
            return camera;
        }

        private void OnLookApplied() {
            if (!_cutsceneModel.IsAvailable) {
                ForgetPlayback();
                return;
            }

            CutscenePlayback playback = _cutsceneModel.Playback;
            double now = NetworkTime.time;
            if (_cutsceneLocalModel.IsPlaying && _cutscenePlaybackRule.ShouldEnd(playback, _cutsceneLocalModel.PlayingSequence, now))
                EndPlaying(_cutsceneConfiguration.ReturnBlendSeconds);

            if (!_cutsceneLocalModel.IsPlaying && _cutscenePlaybackRule.ShouldStart(playback, _cutsceneLocalModel.LastPlayedSequence, now))
                BeginPlaying(playback);

            if (_cutsceneLocalModel.IsPlaying)
                PlaceCamera(playback, now);
        }

        private void BeginPlaying(in CutscenePlayback playback) {
            Transform view = _gameCameraService.OutputCamera.transform;
            _cutsceneLocalModel.Begin(playback.Sequence, new Pose(view.position, view.rotation));
            _camera.transform.SetPositionAndRotation(view.position, view.rotation);
            _gameCameraService.BeginOverrideView(_camera, CUT_SECONDS);
        }

        private void EndPlaying(float blendSeconds) {
            _cutsceneLocalModel.End();
            _gameCameraService.EndOverrideView(_camera, blendSeconds);
        }

        private void ForgetPlayback() {
            if (_cutsceneLocalModel.IsPlaying)
                _gameCameraService.EndOverrideView(_camera, CUT_SECONDS);

            _cutsceneLocalModel.Forget();
        }

        private void PlaceCamera(in CutscenePlayback playback, double now) {
            ResolveFocuses(playback.Shots);
            Pose pose = _cutsceneCameraPath.Evaluate(playback.Shots, _focuses, _cutsceneLocalModel.StartPose, _cutscenePlaybackRule.GetElapsedSeconds(playback, now));
            _camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private void ResolveFocuses(IReadOnlyList<CutsceneShot> shots) {
            _focuses.Clear();
            for (int i = 0; i < shots.Count; i++) {
                CutsceneShot shot = shots[i];
                _focuses.Add(shot.Kind == CutsceneShotKind.FollowTarget && TryFindTarget(shot.TargetId, out Vector3 target) ? target : shot.Focus);
            }
        }

        private bool TryFindTarget(string targetId, out Vector3 position) {
            position = Vector3.zero;
            for (int i = 0; i < _cutsceneTargetSources.Count; i++) {
                if (_cutsceneTargetSources[i].TryGetTargetPosition(targetId, out position))
                    return true;
            }

            return false;
        }
    }
}
