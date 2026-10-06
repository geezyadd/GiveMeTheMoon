using Features.CharacterMovableModule.Scripts.Models;
using Features.GrabModule.Scripts;
using Features.NetworkModelModule.Scripts;
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
        [SerializeField] private Rigidbody _body;
        [SerializeField] private NetworkRigidbodyUnreliable _networkBody;
        [SerializeField] private Transform _followTarget;
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private Collider[] _colliders;

        private IPlayerBodyRegistry _registry;
        private PlayerControlBlockModel _controlBlock;
        private IShipRiderRelease _shipRiderRelease;
        private IHeldItemRelease _heldItemRelease;
        private IReadOnlyPlayerLifeModel _clientLife;
        private RigidbodyConstraints _aliveConstraints;
        private bool _deadShown;
        private bool _controlBlocked;

        public IDamageable Damageable =>
            _damageable;

        public IReadOnlyPlayerLifeModel Life =>
            _damageable.Life;

        public PlayerKey Key =>
            _damageable.Key;

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

        private void Awake() {
            _aliveConstraints = _body.constraints;
            _heldItemRelease = GetComponent<IHeldItemRelease>();
        }

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

        // Every peer, the owner whose client moves the body included, jumps straight to the pose instead of
        // interpolating there from where the player died.
        [Server]
        public void ServerTeleport(Pose pose) =>
            _networkBody.ServerTeleport(pose.position, pose.rotation);

        [Server]
        public void ServerApplyDeath() {
            _heldItemRelease.ServerReleaseHeld();
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
            StopBody();
            if (dead) {
                _body.constraints = RigidbodyConstraints.FreezeAll;
                return;
            }

            // A revived body was already moved to its spawn while frozen and starts from rest. An interpolated body
            // keeps blending from the pose of the last physics step (where it died) until the next one, even after
            // the transform is set: switching interpolation off and back drops that old pose.
            RigidbodyInterpolation interpolation = _body.interpolation;
            _body.interpolation = RigidbodyInterpolation.None;
            transform.SetPositionAndRotation(_body.position, _body.rotation);
            _body.interpolation = interpolation;
            _body.constraints = _aliveConstraints;
        }

        private void StopBody() {
            if (_body.isKinematic)
                return;

            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Command]
        public void CmdDebugKill() =>
            _damageable.ServerKill(DamageType.Generic);
#endif
    }
}
