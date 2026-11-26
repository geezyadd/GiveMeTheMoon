using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Scripts.Core;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Hits;
using Features.WeaponModule.Scripts.Impacts;
using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.Holding;
using Features.WeaponModule.Scripts.Projectiles;
using Features.WeaponModule.Scripts.Shooting;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.WeaponModule.Scripts.Network {
    // A player's weapon on the network, one path for every weapon. A hand or a turret equips the weapon on every peer.
    // On the holder's own peer the weapon system pulls the trigger: each shot is shown at once and sent to the server
    // (CmdShot), which checks it against what it knows (holding, rounds, the hit rule), applies a target hit, launches
    // the real projectile and tells every other peer, which shows the same shot (RpcShot). The trigger and the aim are
    // also synced, throttled, for what lasts between shots (barrel spin, the weapon's pose in other players' views).
    public sealed class WeaponHolderSync : NetworkBehaviour {
        [SyncVar]
        private bool _isTriggerHeld;

        // In the holder's own frame (a turret: its base), so the ship's motion does not count as aiming.
        [SyncVar]
        private Vector3 _aimDirection = Vector3.forward;

        private readonly WeaponHolderColliders _ownColliders = new WeaponHolderColliders();

        private IWeaponHitService _weaponHitService;
        private IPlayerIdentityService _playerIdentityService;
        private IWeaponShooterFactory _weaponShooterFactory;
        private IWeaponCatalogIndexService _weaponCatalogIndexService;
        private IWeaponService _weaponService;
        private IWeaponImpactLookup _impactLookup;
        private IProjectileSpawner _projectileSpawner;
        private WeaponShotPresenter _shotPresenter;
        private WeaponCatalog _weaponCatalog;

        private IWeapon _weapon;
        private IWeaponAim _aim;
        private WeaponShooter _shooter;
        private ServerWeaponHolding _serverHolding;
        private int _weaponIndex = -1;
        private ushort _sequence;
        private bool _isSentTriggerHeld;
        private bool _isDebugTriggerHeld;
        private Vector3 _ownerFrameAim = Vector3.forward;

        public bool IsTriggerHeld => _isTriggerHeld;
        // The trigger as the holder's own peer last sent it.
        public bool OwnerIsTriggerHeld => _isSentTriggerHeld;
        public Vector3 AimDirection => _aimDirection;
        public IWeapon Weapon => IsAlive(_weapon) ? _weapon : null;

        // How ready the held weapon's next shot is on the holder's peer: the reload while one runs.
        public float OwnerReadiness {
            get {
                if (_shooter == null)
                    return 0f;

                if (_weapon.TryGetCapability(out IWeaponMagazine magazine)) {
                    if (magazine.IsReloading)
                        return magazine.ReloadProgress;

                    if (magazine.HasRound == false)
                        return 0f;
                }

                return _shooter.Readiness;
            }
        }

        [Inject]
        private void InjectDependencies(
            IWeaponHitService weaponHitService,
            IPlayerIdentityService playerIdentityService,
            IWeaponShooterFactory weaponShooterFactory,
            IWeaponCatalogIndexService weaponCatalogIndexService,
            IWeaponService weaponService,
            IWeaponImpactLookup impactLookup,
            IProjectileSpawner projectileSpawner,
            WeaponShotPresenter shotPresenter,
            WeaponCatalog weaponCatalog) {
            _weaponHitService = weaponHitService;
            _playerIdentityService = playerIdentityService;
            _weaponShooterFactory = weaponShooterFactory;
            _weaponCatalogIndexService = weaponCatalogIndexService;
            _weaponService = weaponService;
            _impactLookup = impactLookup;
            _projectileSpawner = projectileSpawner;
            _shotPresenter = shotPresenter;
            _weaponCatalog = weaponCatalog;
        }

        private void OnDestroy() =>
            _weaponService?.Unequip(this);

        // Every other peer moves the held weapon's fire mode from the synced trigger, only for what lasts between
        // shots (a minigun's barrels spin up); its shots come from the server.
        private void Update() {
            if (_shooter == null || isOwned || IsAlive(_weapon) == false)
                return;

            _shooter.Advance(_isTriggerHeld && _weapon.IsUsable, Time.deltaTime, _weapon.Feel);
        }

        // On every peer, by what holds the weapon (a hand, a turret seat). The latest one wins: a turret seat takes over
        // from a weapon in the hand, which the hand equips again once the holder is free (HandWeaponWielder).
        public void Equip(IWeapon weapon, IWeaponAim aim) {
            ClearHolding();
            _weapon = weapon;
            _aim = aim;
            _weaponIndex = _weaponCatalogIndexService.GetIndex(weapon.Definition);
            _shooter = _weaponShooterFactory.Create(weapon.Definition);
            _ownColliders.Collect(_impactLookup, netId, weapon as Object);
            if (isServer)
                _serverHolding = new ServerWeaponHolding(weapon.Definition, _weaponIndex, _playerIdentityService.GetKey(connectionToClient), aim);

            if (isOwned == false)
                return;

            _isSentTriggerHeld = false;
            _weaponService.Equip(weapon, this);
            weapon.Feel.SetViewOwned(true);
        }

        // Lets go of this weapon only: what holds another one (a turret seat over a hand) keeps it. The weapon may
        // already be gone (installed, destroyed with its scene) when it is let go.
        public void Unequip(IWeapon weapon) {
            if (_shooter != null && ReferenceEquals(weapon, _weapon))
                ClearHolding();
        }

        private void ClearHolding() {
            if (_shooter == null)
                return;

            if (isOwned) {
                if (_isSentTriggerHeld)
                    SendAim(_ownerFrameAim, false);

                _weaponService.Unequip(this);
                if (IsAlive(_weapon))
                    _weapon.Feel.SetViewOwned(false);
            }

            if (isServer) {
                _serverHolding = null;
                _isTriggerHeld = false;
            }

            _weapon = null;
            _aim = null;
            _shooter = null;
            _weaponIndex = -1;
            _isDebugTriggerHeld = false;
            _ownColliders.Clear();
        }

        // The holder's aim in its own frame, kept by what holds the weapon; it goes out with the next trigger change.
        public void OwnerSetAim(Vector3 frameAimDirection) =>
            _ownerFrameAim = frameAimDirection;

        // A holder that aims a lot (a turret) sends its aim on its own schedule too.
        public void OwnerSendAim(Vector3 frameAimDirection, bool isTriggerHeld) {
            _ownerFrameAim = frameAimDirection;
            SendAim(frameAimDirection, isTriggerHeld);
        }

        public void OwnerAdvance(bool isTriggerHeld, float deltaTime) {
            if (_shooter == null || IsAlive(_weapon) == false)
                return;

            bool isHeld = (isTriggerHeld || _isDebugTriggerHeld) && _weapon.IsUsable;
            bool hasMagazine = _weapon.TryGetCapability(out IWeaponMagazine magazine);
            int shots = _shooter.Advance(isHeld && (hasMagazine == false || magazine.HasRound), deltaTime, _weapon.Feel);
            for (int i = 0; i < shots; i++) {
                if (hasMagazine) {
                    if (magazine.HasRound == false)
                        break;

                    magazine.SpendRound();
                }

                FireOwnerShot();
            }

            if (isHeld != _isSentTriggerHeld)
                SendAim(_ownerFrameAim, isHeld);
        }

        public void OwnerRequestReload() {
            if (_shooter != null && _weapon.TryGetCapability(out IWeaponMagazine magazine) && magazine.IsReloading == false
                && magazine.Rounds < magazine.Capacity)
                CmdReload();
        }

        // Test hook for the loop autotest: holds the trigger of the weapon the owner holds.
        public void DebugHoldTrigger(bool isHeld) =>
            _isDebugTriggerHeld = isHeld;

        private void FireOwnerShot() {
            WeaponShotOutcome outcome = _shooter.Fire(_aim.Origin, _aim.Direction, netId);
            WeaponShotMessage message = WeaponShotMessage.From(outcome, _weaponIndex, ++_sequence);
            _shotPresenter.Present(_weapon.Definition, _weapon.Feel, outcome, ProjectileId(message.Sequence), spawnsCopy: isServer == false,
                _ownColliders.Colliders);
            CmdShot(message);
        }

        private void SendAim(Vector3 frameAimDirection, bool isTriggerHeld) {
            _isSentTriggerHeld = isTriggerHeld;
            if (isServer) {
                ServerSetAim(frameAimDirection, isTriggerHeld);
                return;
            }

            CmdSetAim(frameAimDirection, isTriggerHeld);
        }

        // Only a player who holds a weapon on the server may fire it.
        private void ServerSetAim(Vector3 aimDirection, bool isTriggerHeld) {
            _isTriggerHeld = isTriggerHeld && _serverHolding != null;
            _aimDirection = aimDirection;
        }

        [Command]
        private void CmdSetAim(Vector3 aimDirection, bool isTriggerHeld) =>
            ServerSetAim(aimDirection, isTriggerHeld);

        [Command]
        private void CmdReload() {
            if (_serverHolding != null && IsAlive(_weapon) && _weapon.TryGetCapability(out IWeaponMagazine magazine))
                magazine.ServerBeginReload();
        }

        [Command]
        private void CmdShot(WeaponShotMessage message) {
            if (_serverHolding == null || IsAlive(_weapon) == false || message.WeaponIndex != _weaponIndex)
                return;

            if (_weapon.TryGetCapability(out IWeaponMagazine magazine) && magazine.ServerTrySpendRound() == false)
                return;

            if (message.IsTargetHit)
                _weaponHitService.ServerApplyReportedHit(_serverHolding, message.WeaponIndex,
                    new WeaponHitReport(message.TargetId, message.EndPoint, message.Normal), NetworkTime.time);

            ProjectileDefinition projectile = _serverHolding.Weapon.Projectile;
            if (projectile != null)
                _projectileSpawner.ServerSpawn(projectile, message.Origin, message.Direction * projectile.Speed, ProjectileId(message.Sequence),
                    _ownColliders.Colliders, ServerEndProjectile);

            RpcShot(message);
        }

        [ClientRpc(includeOwner = false)]
        private void RpcShot(WeaponShotMessage message) {
            if (message.WeaponIndex < 0 || message.WeaponIndex >= _weaponCatalog.Weapons.Count)
                return;

            WeaponDefinition weapon = _weaponCatalog.Weapons[message.WeaponIndex];
            bool isHeldHere = _shooter != null && IsAlive(_weapon) && _weaponIndex == message.WeaponIndex;
            WeaponShotOutcome outcome = _shotPresenter.Rebuild(weapon, message);
            _shotPresenter.Present(weapon, isHeldHere ? _weapon.Feel : null, outcome, ProjectileId(message.Sequence), spawnsCopy: isServer == false,
                _ownColliders.Colliders);
        }

        // The holder object may be gone (its player left) by the time a projectile it launched goes off.
        private void ServerEndProjectile(ulong projectileId) {
            if (this != null && NetworkServer.active)
                RpcProjectileEnded(projectileId);
        }

        [ClientRpc]
        private void RpcProjectileEnded(ulong projectileId) {
            if (isServer == false)
                _projectileSpawner.EndCopy(projectileId);
        }

        private ulong ProjectileId(ushort sequence) =>
            ((ulong)netId << 16) | sequence;

        private static bool IsAlive(IWeapon weapon) =>
            weapon != null && (weapon is not Object unityObject || unityObject != null);
    }
}
