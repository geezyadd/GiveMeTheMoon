using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Scripts.Definitions;
using Features.WeaponModule.Scripts.Hits;
using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.Holding;
using Features.WeaponModule.Scripts.Shooting;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.WeaponModule.Scripts.Network {
    // A player's weapon on the network, one path for every weapon: the owner sends whether the trigger is held and
    // where it aims (the holder throttles it), and its target hits in batches at most once per network tick; the
    // server checks every hit against what it knows about the holding. Other peers read the trigger and the aim and
    // replay the fire themselves: no message per bullet.
    public sealed class WeaponHolderSync : NetworkBehaviour, IWeaponHitReporter {
        [SyncVar]
        private bool _isTriggerHeld;

        // In the holder's own frame (a turret: its base), so the ship's motion does not count as aiming.
        [SyncVar]
        private Vector3 _aimDirection = Vector3.forward;

        private readonly WeaponHitQueue _pendingHits = new WeaponHitQueue();

        private IWeaponHitService _weaponHitService;
        private IPlayerIdentityService _playerIdentityService;
        private ServerWeaponHolding _serverHolding;
        private int _ownerWeaponIndex = -1;
        private double _nextHitSendTime;

        public bool IsTriggerHeld => _isTriggerHeld;
        public Vector3 AimDirection => _aimDirection;

        [Inject]
        private void InjectDependencies(IWeaponHitService weaponHitService, IPlayerIdentityService playerIdentityService) {
            _weaponHitService = weaponHitService;
            _playerIdentityService = playerIdentityService;
        }

        private void LateUpdate() {
            if (isOwned == false || _pendingHits.Count == 0 || NetworkTime.localTime < _nextHitSendTime)
                return;

            _nextHitSendTime = NetworkTime.localTime + NetworkClient.sendInterval;
            CmdReportHits(_pendingHits.DequeueBatch(_ownerWeaponIndex));
        }

        public void OwnerBeginHolding(int weaponIndex) {
            _ownerWeaponIndex = weaponIndex;
            _pendingHits.Clear();
        }

        public void OwnerEndHolding() {
            _ownerWeaponIndex = -1;
            _pendingHits.Clear();
        }

        public void OwnerSendAim(Vector3 aimDirection, bool isTriggerHeld) {
            if (isServer) {
                ServerSetAim(aimDirection, isTriggerHeld);
                return;
            }

            CmdSetAim(aimDirection, isTriggerHeld);
        }

        public void ReportTargetHit(in WeaponShotOutcome outcome) =>
            _pendingHits.TryEnqueue(new WeaponHitReport(outcome.TargetId, outcome.EndPoint, outcome.Normal));

        [Server]
        public void ServerBeginHolding(WeaponDefinition weapon, int weaponIndex, IWeaponAim aim) =>
            _serverHolding = new ServerWeaponHolding(weapon, weaponIndex, _playerIdentityService.GetKey(connectionToClient), aim);

        [Server]
        public void ServerEndHolding() {
            _serverHolding = null;
            _isTriggerHeld = false;
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
        private void CmdReportHits(WeaponHitBatch batch) {
            if (_serverHolding == null)
                return;

            for (int i = 0; i < batch.Count && i < WeaponHitBatch.CAPACITY; i++)
                _weaponHitService.ServerApplyReportedHit(_serverHolding, batch.WeaponIndex, batch.Get(i), NetworkTime.time);
        }
    }
}
