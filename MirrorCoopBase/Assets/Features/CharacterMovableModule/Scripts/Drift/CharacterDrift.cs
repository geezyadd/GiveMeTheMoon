using Features.FloatingControllerModule;
using UnityEngine;
using Zenject;

namespace Features.CharacterMovableModule.Scripts.Drift {
    // A player's body drifting in open space, on every peer. The owner's peer simulates it: no gravity and no hover,
    // the velocity fades to a slow drift, the move keys give a very weak push along the view and a pull (a rope) or a
    // push (a shot's recoil) comes from outside; the network body carries the result to the others as usual. Every
    // peer shows the slow tumble and the dangling limbs on its own. The walking body does not simulate meanwhile.
    public sealed class CharacterDrift : MonoBehaviour {
        [SerializeField] private Rigidbody _body;
        [SerializeField] private FloatingController _floatingController;
        [Tooltip("The visual that tumbles; the colliders and the camera anchors stay upright.")]
        [SerializeField] private Transform _tumbleRoot;
        [Tooltip("The point the visual tumbles round, in the tumbling visual's parent space (the body's middle).")]
        [SerializeField] private Vector3 _tumbleCenter = Vector3.up;
        [Tooltip("The arms and legs that dangle while drifting; shown only then.")]
        [SerializeField] private Transform[] _limbs = new Transform[0];

        private readonly CharacterDriftMotion _motion = new CharacterDriftMotion();
        private readonly CharacterDriftTumble _tumble = new CharacterDriftTumble();

        private CharacterInputBuffer _characterInputBuffer;
        private CharacterDriftSettings _settings;
        private Quaternion[] _limbRestRotations;
        private Quaternion _tumbleRestRotation;
        private Vector3 _tumbleRestPosition;
        private bool _isOwner;
        private bool _wasUsingGravity;
        private bool _isPulled;
        private Vector3 _pullVelocity;
        private Vector3 _pendingPush;

        public bool IsDrifting { get; private set; }

        [Inject]
        private void InjectDependencies(CharacterInputBuffer characterInputBuffer) =>
            _characterInputBuffer = characterInputBuffer;

        private void Awake() {
            _tumbleRestRotation = _tumbleRoot.localRotation;
            _tumbleRestPosition = _tumbleRoot.localPosition;
            _limbRestRotations = new Quaternion[_limbs.Length];
            for (int i = 0; i < _limbs.Length; i++)
                _limbRestRotations[i] = _limbs[i].localRotation;

            ShowLimbs(false);
        }

        private void OnDisable() =>
            End();

        private void FixedUpdate() {
            if (IsDrifting == false || _isOwner == false || _body.isKinematic)
                return;

            _body.linearVelocity = _isPulled
                ? _pullVelocity
                : _motion.Step(_body.linearVelocity + _pendingPush, ReadPushDirection(), _settings, Time.fixedDeltaTime);
            _pendingPush = Vector3.zero;
        }

        private void LateUpdate() {
            if (IsDrifting == false)
                return;

            _tumble.Advance(_settings, Time.deltaTime);
            Quaternion tumble = _tumble.BodyRotation;
            _tumbleRoot.localRotation = tumble * _tumbleRestRotation;
            _tumbleRoot.localPosition = _tumbleCenter + tumble * (_tumbleRestPosition - _tumbleCenter);
            for (int i = 0; i < _limbs.Length; i++)
                _limbs[i].localRotation = _limbRestRotations[i] * Quaternion.Euler(_tumble.LimbSwingDegrees(_settings, (float)i / _limbs.Length), 0f, 0f);
        }

        // The owner keeps the body's velocity at this moment (the world exit velocity); every peer starts the tumble.
        public void Begin(CharacterDriftSettings settings, bool isOwner) {
            if (IsDrifting)
                return;

            _settings = settings;
            _isOwner = isOwner;
            IsDrifting = true;
            _isPulled = false;
            _pendingPush = Vector3.zero;
            _tumble.Reset();
            ShowLimbs(true);
            if (_isOwner == false)
                return;

            _wasUsingGravity = _body.useGravity;
            _body.useGravity = false;
            _floatingController.HoverEnabled = false;
        }

        public void End() {
            if (IsDrifting == false)
                return;

            IsDrifting = false;
            _isPulled = false;
            _tumbleRoot.localRotation = _tumbleRestRotation;
            _tumbleRoot.localPosition = _tumbleRestPosition;
            for (int i = 0; i < _limbs.Length; i++)
                _limbs[i].localRotation = _limbRestRotations[i];

            ShowLimbs(false);
            if (_isOwner == false)
                return;

            _body.useGravity = _wasUsingGravity;
            _floatingController.HoverEnabled = true;
        }

        // A one-off change of velocity on the owner's body (a shot's recoil); nothing while walking.
        public void OwnerPush(Vector3 velocityChange) {
            if (IsDrifting && _isOwner)
                _pendingPush += velocityChange;
        }

        // While a rope pulls the body it moves at exactly this velocity; no fade, no push.
        public void OwnerSetPullVelocity(Vector3 velocity) {
            _isPulled = true;
            _pullVelocity = velocity;
        }

        public void OwnerClearPull() =>
            _isPulled = false;

        private Vector3 ReadPushDirection() {
            Transform view = Camera.main != null ? Camera.main.transform : null;
            Vector2 stick = _characterInputBuffer.MoveStick;
            if (view == null || _characterInputBuffer.IsBlocked)
                return Vector3.zero;

            return view.forward * stick.y + view.right * stick.x;
        }

        private void ShowLimbs(bool isShown) {
            for (int i = 0; i < _limbs.Length; i++)
                _limbs[i].gameObject.SetActive(isShown);
        }
    }
}
