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
    // - Fly: the body has been off the ground for the configured delay; never while held.
    // Works only while the ragdoll is awake (its animator is on): the ragdoll samples the animator once a physics step
    // (AnimatedPoseSampler), so the values apply from the next step.
    public sealed class PlayerAnimationDriver : MonoBehaviour {
        [SerializeField] private CharacterMovableBase _movable;
        [SerializeField] private RagdollBody _ragdoll;

        private readonly AirborneDelay _airborneDelay = new();

        private PlayerAnimationConfiguration _configuration;
        private Animator _animator;
        private RagdollGroundSensor _ground;

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
            _animator.SetBool(PlayerAnimatorParameters.Fly, _airborneDelay.Step(isInAir, _configuration.FlyDelaySeconds, deltaTime));
        }

        private float ReadForward() {
            IMovementControls controls = _movable.IsHeld ? null : _movable.ActiveControls;
            float forward = controls != null ? Mathf.Clamp01(controls.ReadWish().magnitude) : 0f;
            return forward < _configuration.ForwardIdleCutoff ? 0f : forward;
        }
    }
}
