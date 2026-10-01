using Features.CharacterMovableModule.Scripts.Models;
using Features.GrabModule.Scripts;
using Features.PlayerLifeModule.Scripts.Generated;
using Features.ShipModule.Scripts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.PlayerLifeModule.Scripts {
    // Shows the synced life state on every peer and carries out the server side of a death.
    // Must sit after PlayerDamageable on the prefab: it reads the model that bridge binds in OnStartServer / OnStartClient.
    public sealed class PlayerLifeBody : NetworkBehaviour, IPlayerLifeActor {
        [SerializeField] private PlayerDamageable _damageable;
        [SerializeField] private GrabController _grab;
        [SerializeField] private Rigidbody _body;
        [SerializeField] private Transform _followTarget;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private Collider[] _colliders;

        private IPlayerBodyRegistry _registry;
        private PlayerControlBlockModel _controlBlock;
        private IShipRiderRelease _shipRiderRelease;
        private IReadOnlyPlayerLifeModel _clientLife;
        private RigidbodyConstraints _aliveConstraints;
        private bool _deadShown;
        private bool _controlBlocked;

        public IDamageable Damageable =>
            _damageable;

        public IReadOnlyPlayerLifeModel Life =>
            _damageable.Life;

        public Transform FollowTarget =>
            _followTarget;

        public PlayerLifeState LifeState =>
            _damageable.Life.LifeState;

        [Inject]
        private void InjectDependencies(
            IPlayerBodyRegistry registry,
            PlayerControlBlockModel controlBlock,
            IShipRiderRelease shipRiderRelease) {
            _registry = registry;
            _controlBlock = controlBlock;
            _shipRiderRelease = shipRiderRelease;
        }

        private void Awake() =>
            _aliveConstraints = _body.constraints;

        public override void OnStartServer() =>
            _registry.AddServer(this);

        public override void OnStopServer() =>
            _registry.RemoveServer(this);

        public override void OnStartClient() {
            _clientLife = _damageable.Life;
            _clientLife.OnLifeStateChanged += OnLifeStateChanged;
            ApplyLifeState();
            _registry.AddClient(this, isLocalPlayer);
        }

        public override void OnStartLocalPlayer() {
            ApplyLifeState();
            _registry.AddClient(this, true);
        }

        public override void OnStopClient() {
            _clientLife.OnLifeStateChanged -= OnLifeStateChanged;
            _clientLife = null;
            SetControlBlocked(false);
            _registry.RemoveClient(this);
        }

        [Server]
        public void ServerSetLifeState(PlayerLifeState state) =>
            _damageable.ServerSetLifeState(state);

        [Server]
        public void ServerApplyDeath() {
            _grab.ServerReleaseHeld();
            _shipRiderRelease.ServerReleaseRider(netIdentity);
        }

        private void OnLifeStateChanged() =>
            ApplyLifeState();

        private void ApplyLifeState() {
            bool dead = _clientLife.LifeState == PlayerLifeState.Dead;
            SetControlBlocked(dead && isLocalPlayer);
            if (dead == _deadShown)
                return;

            _deadShown = dead;
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].enabled = dead == false;

            for (int i = 0; i < _colliders.Length; i++)
                _colliders[i].enabled = dead == false;

            // Collisions are off while dead, so the body is pinned in place instead of falling forever.
            if (dead) {
                if (_body.isKinematic == false) {
                    _body.linearVelocity = Vector3.zero;
                    _body.angularVelocity = Vector3.zero;
                }

                _body.constraints = RigidbodyConstraints.FreezeAll;
                return;
            }

            _body.constraints = _aliveConstraints;
        }

        private void SetControlBlocked(bool blocked) {
            if (_controlBlocked == blocked)
                return;

            _controlBlocked = blocked;
            if (blocked)
                _controlBlock.Request(this);
            else
                _controlBlock.Release(this);
        }
    }
}
