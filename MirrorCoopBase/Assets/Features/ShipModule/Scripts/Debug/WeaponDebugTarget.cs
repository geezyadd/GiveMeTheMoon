#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Features.WeaponModule.Contracts;
using Mirror;
using TMPro;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Debug {
    // A floating test dummy for weapons, spawned from the debug window in editor / development builds (WeaponDebugTargetSystem).
    // It keeps its place in front of the ship and sways from side to side, counts the hits the server accepted and
    // flashes on each one on every peer. Its prefab is registered with Mirror at runtime (WeaponDebugTargetPrefabSystem).
    public sealed class WeaponDebugTarget : NetworkBehaviour, IWeaponTarget, IWeaponTargetCollider {
        // Debug targets keep to their own id range, clear of the field rocks' ids.
        private const ulong TARGET_ID_BASE = 0xDEB0000000000000UL;
        private const string HITS_FORMAT = "{0}";
        private const float FULL_TURN_RADIANS = 2f * Mathf.PI;

        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer _body;
        [SerializeField] private TextMeshPro _hitsLabel;
        [SerializeField] private Color _restColor = Color.white;
        [SerializeField] private Color _flashColor = Color.red;
        [SerializeField, Min(0f)] private float _flashSeconds = 0.08f;
        [SerializeField, Min(0f)] private float _swayMeters = 6f;
        [SerializeField, Min(0.1f)] private float _swayPeriodSeconds = 6f;

        [SyncVar(hook = nameof(OnHitsChanged))]
        private int _hits;

        [SyncVar]
        private NetworkIdentity _shipPose;

        [SyncVar]
        private Vector3 _shipOffset;

        private IWeaponTargetRegistry _weaponTargetRegistry;
        private MaterialPropertyBlock _colors;
        private float _flashLeft;

        public bool IsAlive => true;
        public ulong TargetId => TARGET_ID_BASE + netId;
        public int Hits => _hits;

        [Inject]
        private void InjectDependencies(IWeaponTargetRegistry weaponTargetRegistry) =>
            _weaponTargetRegistry = weaponTargetRegistry;

        private void Awake() =>
            _colors = new MaterialPropertyBlock();

        public override void OnStartServer() =>
            _weaponTargetRegistry.Register(TargetId, this);

        public override void OnStopServer() =>
            _weaponTargetRegistry.Unregister(TargetId);

        private void LateUpdate() {
            FollowShip();
            _flashLeft = Mathf.Max(0f, _flashLeft - Time.deltaTime);
            _colors.SetColor(_baseColorId, _flashLeft > 0f ? _flashColor : _restColor);
            _body.SetPropertyBlock(_colors);
        }

        [Server]
        public void ServerPlace(NetworkIdentity shipPose, Vector3 shipOffset) {
            _shipPose = shipPose;
            _shipOffset = shipOffset;
        }

        public void ServerReceiveHit(in WeaponHit hit) =>
            _hits++;

        // Every peer places it from the ship's pose and the shared network time, so it needs no transform sync.
        private void FollowShip() {
            if (_shipPose == null)
                return;

            float phase = (float)(NetworkTime.time / _swayPeriodSeconds) * FULL_TURN_RADIANS;
            Transform ship = _shipPose.transform;
            transform.SetPositionAndRotation(ship.TransformPoint(_shipOffset + Vector3.right * (Mathf.Sin(phase) * _swayMeters)), ship.rotation);
        }

        private void OnHitsChanged(int previous, int current) {
            _flashLeft = _flashSeconds;
            _hitsLabel.SetText(HITS_FORMAT, current);
        }
    }
}
#endif
