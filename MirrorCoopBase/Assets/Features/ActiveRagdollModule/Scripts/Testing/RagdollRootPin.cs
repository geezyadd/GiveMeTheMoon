using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Test helper: holds the root limb still in the air (kinematic), so the pose following can be checked before
    // the body can stand on its own. Toggle it in Play mode.
    [RequireComponent(typeof(RagdollBody))]
    public sealed class RagdollRootPin : MonoBehaviour {
        [SerializeField] private bool _pinned = true;

        private RagdollBody _body;

        private void Awake() {
            _body = GetComponent<RagdollBody>();
        }

        private void Start() {
            Apply();
        }

        private void OnValidate() {
            if (Application.isPlaying && _body != null)
                Apply();
        }

        private void Apply() {
            RagdollLimb root = _body.Root;
            if (root != null)
                root.Rigidbody.isKinematic = _pinned;
        }
    }
}
