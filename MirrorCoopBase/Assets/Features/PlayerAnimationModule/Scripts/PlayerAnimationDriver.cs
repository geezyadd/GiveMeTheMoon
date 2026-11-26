using Features.ActiveRagdollModule;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.MovementControls;
using UnityEngine;
using Zenject;

namespace Features.PlayerAnimationModule {
    // Sets the player's animator parameters every frame, on every peer that simulates the body (the owner's own and the
    // copies of the others), from that peer's controls and ground sensor; nothing extra goes over the network.
    // - Forward: the walk input, not the measured speed (on a moving, rocking carrier the body always drifts a little
    //   and would shuffle); weak input is cut so the idle stays still; a held player (a seat) stands still.
    // - Fly: the body has been off the ground for the configured delay; never while held. The fall layer's weight
    //   fades in with it and out when it is off.
    // - An action's trigger (an attack's, from the AttackModule's Attacker) fires through Play while the animator runs.
    // Works only while the ragdoll is awake (its animator is on): the ragdoll samples the animator once a physics step
    // (AnimatedPoseSampler), so the values apply from the next step.
    public sealed class PlayerAnimationDriver : MonoBehaviour {
        [SerializeField] private CharacterMovableBase _movable;
        [SerializeField] private RagdollBody _ragdoll;

        private readonly AirborneDelay _airborneDelay = new();

        private PlayerAnimationConfiguration _configuration;
        private Animator _animator;
        private RagdollGroundSensor _ground;
        private RuntimeAnimatorController _layerController;
        private int _flyLayer = -1;

        // Whether the animator runs, so an action played now shows (the ragdoll is awake).
        public bool CanPlay => _animator != null && _animator.isActiveAndEnabled;

        [Inject]
        private void Construct(PlayerAnimationConfiguration configuration) =>
            _configuration = configuration;

        private void Awake() {
            AnimatedPoseSampler sampler = _ragdoll.GetComponent<AnimatedPoseSampler>();
            _animator = sampler != null && sampler.Animator != null
                ? sampler.Animator
                : _ragdoll.GetComponentInChildren<Animator>();
            _ground = _ragdoll.GetComponent<RagdollGroundSensor>();
        }

        private void Update() {
            if (_ragdoll == null || _animator == null || _animator.isActiveAndEnabled == false) {
                _airborneDelay.Reset();
                return;
            }

            float deltaTime = Time.deltaTime;
            _animator.SetFloat(PlayerAnimatorParameters.Forward, ReadForward(), _configuration.ForwardDampingSeconds, deltaTime);
            bool isInAir = _movable.IsHeld == false && _ground.IsGrounded == false;
            bool isFlying = _airborneDelay.Step(isInAir, _configuration.FlyDelaySeconds, deltaTime);
            _animator.SetBool(PlayerAnimatorParameters.Fly, isFlying);
            FadeFlyLayer(isFlying, deltaTime);
        }

        // Fires an action's trigger (an attack's); nothing while the animator does not run.
        public void Play(int triggerHash) {
            if (CanPlay)
                _animator.SetTrigger(triggerHash);
        }

        // The fall layer over the locomotion fades in while flying and out on the feet; at weight 0 it changes nothing.
        private void FadeFlyLayer(bool isFlying, float deltaTime) {
            int layer = FlyLayerIndex();
            if (layer < 0)
                return;

            float seconds = isFlying ? _configuration.FlyBlendInSeconds : _configuration.FlyBlendOutSeconds;
            float target = isFlying ? 1f : 0f;
            float weight = seconds > 0f
                ? Mathf.MoveTowards(_animator.GetLayerWeight(layer), target, deltaTime / seconds)
                : target;
            _animator.SetLayerWeight(layer, weight);
        }

        // Looked up again only when the animator gets another controller; -1 when it has no fall layer.
        private int FlyLayerIndex() {
            RuntimeAnimatorController controller = _animator.runtimeAnimatorController;
            if (controller != _layerController) {
                _layerController = controller;
                _flyLayer = controller != null ? _animator.GetLayerIndex(PlayerAnimatorParameters.FlyLayerName) : -1;
            }

            return _flyLayer;
        }

        private float ReadForward() {
            IMovementControls controls = _movable.IsHeld ? null : _movable.ActiveControls;
            float forward = controls != null ? Mathf.Clamp01(controls.ReadWish().magnitude) : 0f;
            return forward < _configuration.ForwardIdleCutoff ? 0f : forward;
        }
    }
}
