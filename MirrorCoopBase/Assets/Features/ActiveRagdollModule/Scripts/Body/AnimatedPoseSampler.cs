using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;

namespace Features.ActiveRagdollModule {
    // Runs the body's animator by hand once per physics step and keeps the pose only as limb targets:
    // the animator moves the bones, every limb remembers where it was told to be, then the limbs go straight back
    // to their rigidbodies. Bones that are not limbs (fingers, neck) keep the animated pose and ride on their limb.
    // Runs before anything that applies forces in the same step.
    // An optional rig (Animation Rigging constraints, e.g. an arm reaching for a target) is built into the animator's
    // own graph, so one evaluation runs the animation and then the rig, and the limbs follow the result. (A rig graph
    // of its own, evaluated apart, starts from the avatar's default pose, not from the animation: the body sinks.)
    [DefaultExecutionOrder(-95)]
    [RequireComponent(typeof(RagdollBody))]
    public sealed class AnimatedPoseSampler : MonoBehaviour {
        [Tooltip("Empty: the first animator under this object.")]
        [SerializeField] private Animator _animator;
        [Tooltip("Optional: the constraints over the animation, on the animator's object. Keep it turned off: this component builds it into the animator's graph.")]
        [SerializeField] private RigBuilder _rig;

        private RagdollBody _body;
        private PlayableGraph _graph;
        private bool _graphReady;
        private bool _firstStepSkipped;
        private bool _missingGraphReported;
        private bool _rigReady;
        private PlayableGraph _rigGraph;
        private int _rigFirstOutput;
        private Vector3 _animatedRootShift;

        public Animator Animator => _animator;

        // On every sample with a rig, just before it: the offset from the root limb's rigidbody to where the previous
        // sample's animated pose put the root limb. A world target of the rig (another body's head) is moved by it, so
        // it stands to the animated pose as it stands to the real body.
        public event Action<Vector3> RigEvaluating;

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
            TearDownRig();
            _graphReady = false;
            _firstStepSkipped = false;
            _hasShownFacing = false;
        }

        private void OnDestroy() =>
            TearDownRig();

        private void FixedUpdate() {
            // The animator dropped its graph (it was turned off and on): take the new one.
            if (_graphReady && _graph.IsValid() == false) {
                TearDownRig();
                _graphReady = false;
                _firstStepSkipped = false;
            }

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
            BuildRig();
            return true;
        }

        // Built into the animator's graph again each time the animator gets a new one.
        private void BuildRig() {
            TearDownRig();
            if (_rig == null)
                return;

            _rig.enabled = false;
            _rigGraph = _graph;
            _rigFirstOutput = _graph.GetOutputCount();
            _rigReady = _rig.Build(_graph);
        }

        // The rig frees its jobs' memory on Clear but leaves its nodes in a graph it did not create. If that graph lives
        // on (the animator keeps it, or rebinds it), the next evaluation or rig build runs freed jobs and the player
        // crashes. So its outputs and nodes leave the graph first: everything it added after the animator's own outputs.
        private void TearDownRig() {
            _rigReady = false;
            if (_rig == null)
                return;

            if (_rigGraph.IsValid()) {
                for (int i = _rigGraph.GetOutputCount() - 1; i >= _rigFirstOutput; i--) {
                    PlayableOutput output = _rigGraph.GetOutput(i);
                    Playable source = output.GetSourcePlayable();
                    _rigGraph.DestroyOutput(output);
                    if (source.IsValid())
                        _rigGraph.DestroySubgraph(source);
                }
            }

            _rigGraph = default;
            _rig.Clear();
        }

        // Weights and targets set now reach the rig in the evaluation that follows.
        private void PrepareRig() {
            if (_rigReady == false)
                return;

            RigEvaluating?.Invoke(_animatedRootShift);
            _rig.SyncLayers();
        }

        private void SamplePose(float deltaTime) {
            Transform origin = _animator.transform;
            Quaternion rotationBefore = origin.rotation;
            Quaternion facing = FacingYaw.HasValue ? Quaternion.Euler(0f, TurnTowardFacing(FacingYaw.Value, deltaTime), 0f) : rotationBefore;
            origin.rotation = Tilt * facing;

            PrepareRig();
            _graph.Evaluate(deltaTime);

            Vector3 animatedRootPosition = _body.Root.transform.position;
            _animatedRootShift = animatedRootPosition - _body.Root.Rigidbody.position;
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
