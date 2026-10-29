using System;
using Unity.Cinemachine;
using UnityEngine;

namespace Features.CameraModule.Scripts.Services {
    public interface IGameCameraService {
        event Action LookApplied;

        string ActiveId { get; }
        Camera OutputCamera { get; }

        void Register(GameCamera camera);
        void Unregister(GameCamera camera);
        void BindToLocalPlayer(Transform follow, Transform lookAt, Transform eye);
        void RetargetOrbit(Transform follow);
        void HoldOrbit();
        void ClearLocalPlayer();
        void BlendTo(string id, float duration = -1f);
        void ToggleFpAndTp();
        void BeginFixedView(Transform eye);
        void EndFixedView();
        // An outside camera (a cutscene's) takes the view over the player's cameras, blended in over the seconds;
        // ending it blends back to the player's live camera.
        public void BeginOverrideView(CinemachineCamera camera, float blendSeconds);
        public void EndOverrideView(CinemachineCamera camera, float blendSeconds);
        bool TryGet(string id, out GameCamera camera);
    }
}
