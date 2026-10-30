using System;
using Unity.Cinemachine;
using UnityEngine;

namespace Features.CameraModule.Scripts.Services {
    public interface IGameCameraService {
        public event Action LookApplied;

        public string ActiveId { get; }
        public Camera OutputCamera { get; }

        public void Register(GameCamera camera);
        public void Unregister(GameCamera camera);
        public void BindToLocalPlayer(Transform follow, Transform lookAt, Transform eye);
        public void RetargetOrbit(Transform follow);
        public void HoldOrbit();
        public void ClearLocalPlayer();
        public void BlendTo(string id, float duration = -1f);
        public void ToggleFpAndTp();
        public void BeginFixedView(Transform eye);
        public void EndFixedView();
        // An outside camera (a cutscene's) takes the view over the player's cameras, blended in over the seconds;
        // ending it blends back to the player's live camera.
        public void BeginOverrideView(CinemachineCamera camera, float blendSeconds);
        public void EndOverrideView(CinemachineCamera camera, float blendSeconds);
        public bool TryGet(string id, out GameCamera camera);
    }
}
