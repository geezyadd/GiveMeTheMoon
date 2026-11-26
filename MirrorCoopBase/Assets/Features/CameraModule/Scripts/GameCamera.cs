using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace Features.CameraModule.Scripts {
    public enum GameCameraKind {
        ThirdPerson = 0,
        FirstPerson = 1
    }

    public sealed class GameCamera : MonoBehaviour {
        [SerializeField] private string _id;
        [SerializeField] private GameCameraKind _kind;
        [SerializeField] private CinemachineCamera _virtualCamera;

        public string Id => _id;
        public GameCameraKind Kind => _kind;

        public CinemachineCamera VirtualCamera => _virtualCamera;

        public void Bind(Transform follow, Transform lookAt) {
            CinemachineCamera camera = VirtualCamera;
            if (camera == null)
                return;

            CameraTarget target = camera.Target;
            target.TrackingTarget = follow;
            if (lookAt != null && lookAt != follow) {
                target.LookAtTarget = lookAt;
                target.CustomLookAtTarget = true;
            }
            else {
                target.LookAtTarget = null;
                target.CustomLookAtTarget = false;
            }

            camera.Target = target;
            LockFollowToTarget();
        }

        public void SetPriority(int value) {
            CinemachineCamera camera = VirtualCamera;
            if (camera == null)
                return;

            camera.Priority = value;
        }

        public void LockFollowToTarget() {
            CinemachineFollow follow = GetComponent<CinemachineFollow>();
            if (follow == null)
                return;

            TrackerSettings tracker = follow.TrackerSettings;
            tracker.BindingMode = BindingMode.LockToTarget;
            tracker.PositionDamping = Vector3.zero;
            tracker.RotationDamping = Vector3.zero;
            tracker.QuaternionDamping = 0f;
            follow.TrackerSettings = tracker;

            CinemachineRotateWithFollowTarget rotate = GetComponent<CinemachineRotateWithFollowTarget>();
            if (rotate != null)
                rotate.Damping = 0f;
        }

    }
}
