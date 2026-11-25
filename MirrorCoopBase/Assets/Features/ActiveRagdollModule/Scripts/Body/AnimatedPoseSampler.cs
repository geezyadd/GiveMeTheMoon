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
        private bool _missingGraphReported;

        public Animator Animator => _animator;

        // Where the body should face, degrees around the up axis; the animation is sampled turned that way, so the
        // body turns toward it by following the pose. Null: the animator's own facing.
        public float? FacingYaw { get; set; }

        // A world turn of the whole pose on top of the facing (a body flying head first); the body follows it like any
        // pose. Identity: upright.
        public Quaternion Tilt { get; set; } = Quaternion.identity;

        private float _shownFacingYaw;
        private bool _hasShownFacing;

        private void Awake() {
            _body = GetComponent<RagdollBody>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        private void OnDisable() {
            _graphReady = false;
            _firstStepSkipped = false;
            _hasShownFacing = false;
        }

        private void FixedUpdate() {
            if (_graphReady == false && TryTakeGraph() == false)
                return;

            if (_firstStepSkipped == false) {
                _firstStepSkipped = true;
                return;
            }

            SamplePose(Time.fixedDeltaTime);
        }

        // From now on the graph moves only when this component evaluates it. Taken on the first step the animator has a
        // graph, because an animator builds a new one each time it is turned on.
        private bool TryTakeGraph() {
            if (_animator == null || _animator.isActiveAndEnabled == false)
                return false;

            _graph = _animator.playableGraph;
            if (_graph.IsValid() == false) {
                if (_missingGraphReported == false)
                    Debug.LogError($"Ragdoll: the animator of {name} has no graph (no controller?).", this);
                _missingGraphReported = true;
                return false;
            }

            _animator.applyRootMotion = false;
            // The pose is the body's target, not just its look: it is needed off screen too, or an unseen body would
            // only hold whatever shape it is in.
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _graph.Evaluate(0f);
            _graph.Stop();
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _graphReady = true;
            return true;
        }

        private void SamplePose(float deltaTime) {
            Transform origin = _animator.transform;
            Quaternion rotationBefore = origin.rotation;
            Quaternion facing = FacingYaw.HasValue ? Quaternion.Euler(0f, TurnTowardFacing(FacingYaw.Value, deltaTime), 0f) : rotationBefore;
            origin.rotation = Tilt * facing;

            _graph.Evaluate(deltaTime);

            Vector3 animatedRootPosition = _body.Root.transform.position;
            float originHeight = origin.position.y;
            foreach (RagdollLimb limb in _body.Limbs)
                limb.CaptureTarget(animatedRootPosition, originHeight);

            origin.rotation = rotationBefore;
            // Limbs are listed parents first, so moving a parent never drags an already placed child.
            foreach (RagdollLimb limb in _body.Limbs)
                limb.ReturnToPhysics();
        }

        // The facing the pose is sampled at turns toward the wanted one at most at the profile's turn speed, so a fast
        // spin of the look does not whirl the body round (and fling its legs out). 0: it turns at once.
        private float TurnTowardFacing(float wantedYaw, float deltaTime) {
            float maxSpeed = _body.Profile.MaxFacingTurnDegreesPerSecond;
            if (_hasShownFacing == false || maxSpeed <= 0f)
                _shownFacingYaw = wantedYaw;
            else
                _shownFacingYaw = Mathf.MoveTowardsAngle(_shownFacingYaw, wantedYaw, maxSpeed * deltaTime);

            _hasShownFacing = true;
            return _shownFacingYaw;
        }
    }
}
