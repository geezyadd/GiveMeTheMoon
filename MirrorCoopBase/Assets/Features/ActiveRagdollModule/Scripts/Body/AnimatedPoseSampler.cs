using UnityEngine;
using UnityEngine.Playables;

namespace Features.ActiveRagdollModule {
    // Runs the body's animator by hand once per physics step and keeps the pose only as limb targets:
    // the animator moves the bones, every limb remembers where it was told to be, then the limbs go straight back
    // to their rigidbodies. Bones that are not limbs (fingers, neck) keep the animated pose and ride on their limb.
    // Runs before anything that applies forces in the same step.
    [DefaultExecutionOrder(-95)]
    [RequireComponent(typeof(RagdollBody))]
    public sealed class AnimatedPoseSampler : MonoBehaviour {
        [Tooltip("Empty: the first animator under this object.")]
        [SerializeField] private Animator _animator;

        private RagdollBody _body;
        private PlayableGraph _graph;
        private bool _graphReady;
        private bool _firstStepSkipped;

        public Animator Animator => _animator;

        private void Awake() {
            _body = GetComponent<RagdollBody>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        private void Start() {
            _animator.applyRootMotion = false;
            _graph = _animator.playableGraph;
            if (_graph.IsValid() == false) {
                Debug.LogError($"Ragdoll: the animator of {name} has no graph (no controller?).", this);
                return;
            }

            // From now on the graph moves only when this component evaluates it.
            _graph.Evaluate(0f);
            _graph.Stop();
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _graphReady = true;
        }

        private void FixedUpdate() {
            if (_graphReady == false)
                return;

            if (_firstStepSkipped == false) {
                _firstStepSkipped = true;
                return;
            }

            SamplePose(Time.fixedDeltaTime);
        }

        private void SamplePose(float deltaTime) {
            _graph.Evaluate(deltaTime);

            Vector3 animatedRootPosition = _body.Root.transform.position;
            foreach (RagdollLimb limb in _body.Limbs)
                limb.CaptureTarget(animatedRootPosition);

            // Limbs are listed parents first, so moving a parent never drags an already placed child.
            foreach (RagdollLimb limb in _body.Limbs)
                limb.ReturnToPhysics();
        }
    }
}
