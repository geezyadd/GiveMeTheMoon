using Features.PlayerReplicaModule.Configuration;
using UnityEngine;
using Zenject;

namespace Features.PlayerReplicaModule.Visual {
    // Moves a copy's visual root (the child that holds everything seen: the model with the hand, the nameplate, the
    // spectator camera points) by the visual lag offset, so a jump of the body shows as a quick slide. The colliders
    // stay with the body: physics is exact, only the look lags. Applied on top of wherever other code puts the root,
    // late in the frame.
    [DefaultExecutionOrder(1000)]
    public sealed class ReplicaVisualLag : MonoBehaviour {
        [SerializeField] private Transform _visualRoot;

        private readonly VisualLagOffset _offset = new();

        private ReplicaConfiguration _configuration;
        private Vector3 _appliedLocal;

        [Inject]
        private void Construct(ReplicaConfiguration configuration) =>
            _configuration = configuration;

        // The body was moved by this much at once; hide it if it is worth hiding.
        public void HideJump(Vector3 bodyJump) {
            if (_configuration == null)
                return;

            _offset.HideJump(bodyJump, _configuration.VisualCatchUpThreshold, _configuration.VisualCatchUpMaxDistance);
        }

        private void LateUpdate() {
            if (_visualRoot == null)
                return;

            if (_configuration != null)
                _offset.Tick(Time.deltaTime, _configuration.VisualCatchUpSeconds);

            Vector3 local = _visualRoot.parent.InverseTransformVector(_offset.Offset);
            _visualRoot.localPosition += local - _appliedLocal;
            _appliedLocal = local;
        }
    }
}
