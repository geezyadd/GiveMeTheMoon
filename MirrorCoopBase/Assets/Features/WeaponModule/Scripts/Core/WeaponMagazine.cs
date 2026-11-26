using Mirror;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Core {
    // A networked weapon item's magazine, by its weapon's definition (capacity, reload time). The rounds and the
    // reload's end stay with the item, so a dropped gun keeps what is left in it; only the server changes them. The
    // holding peer spends a round at once with each shot and takes the server's count whenever it arrives.
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class WeaponMagazine : NetworkBehaviour, IWeaponMagazine {
        [SyncVar(hook = nameof(HandleRoundsChanged))]
        private int _rounds = -1;

        // NetworkTime when the running reload is over; 0: none runs.
        [SyncVar]
        private double _reloadEndTime;

        private IWeapon _weapon;
        private int _predictedRounds = -1;

        public int Capacity => Weapon != null ? Weapon.Definition.MagazineCapacity : 0;
        public int Rounds => _predictedRounds >= 0 ? _predictedRounds : Mathf.Max(0, _rounds);
        public bool IsReloading => _reloadEndTime > 0d;
        public bool HasRound => IsReloading == false && Rounds > 0;

        public float ReloadProgress {
            get {
                float seconds = Weapon != null ? Weapon.Definition.ReloadSeconds : 0f;
                if (IsReloading == false || seconds <= 0f)
                    return 1f;

                return Mathf.Clamp01(1f - (float)(_reloadEndTime - NetworkTime.time) / seconds);
            }
        }

        private IWeapon Weapon => _weapon ??= GetComponent<IWeapon>();

        public override void OnStartServer() {
            if (_rounds < 0)
                _rounds = Capacity;
        }

        private void Update() {
            if (isServer && IsReloading && NetworkTime.time >= _reloadEndTime) {
                _rounds = Capacity;
                _predictedRounds = _rounds;
                _reloadEndTime = 0d;
            }
        }

        public void SpendRound() =>
            _predictedRounds = Mathf.Max(0, Rounds - 1);

        [Server]
        public bool ServerTrySpendRound() {
            if (IsReloading || _rounds <= 0)
                return false;

            _rounds--;
            _predictedRounds = _rounds;
            return true;
        }

        [Server]
        public void ServerBeginReload() {
            if (IsReloading || _rounds >= Capacity)
                return;

            _reloadEndTime = NetworkTime.time + Mathf.Max(0f, Weapon.Definition.ReloadSeconds);
        }

        private void HandleRoundsChanged(int oldRounds, int newRounds) =>
            _predictedRounds = newRounds;
    }
}
