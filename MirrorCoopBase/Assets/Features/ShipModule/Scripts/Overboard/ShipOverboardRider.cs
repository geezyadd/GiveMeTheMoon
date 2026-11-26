using Features.CharacterMovableModule.Scripts.Body;
using Features.CharacterMovableModule.Scripts.Drift;
using Features.FloatingControllerModule;
using Features.GameCoreModule.Contracts;
using Features.NetworkModelModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Overboard {
    // A player overboard, on every peer. The server decides when it goes overboard, when it is back aboard and when
    // the void takes it (ShipOverboardSystem); the synced state tells every peer, and each peer drifts the body it
    // simulates (CharacterDrift: the owner its own, the others their copy). The ship takes a drifter back on the
    // owner's peer: once it has drifted a moment and is over the deck (the ship's carrier holds it), its body is caught
    // in the air and rides the ship at rest on it; the owner's packets then say it rides the ship, the server sees it
    // aboard and ends the drift, and the body comes down on the deck. The drift is not restored on a rejoin: a player
    // who leaves overboard comes back on the deck like everyone.
    public sealed class ShipOverboardRider : NetworkBehaviour {
        [SerializeField] private ShipRider _rider;
        [SerializeField] private CharacterDrift _drift;

        [SyncVar]
        private double _overboardSince;

        [SyncVar(hook = nameof(OnOverboardChanged))]
        private bool _isOverboard;

        private ShipOverboardConfiguration _shipOverboardConfiguration;
        private IComponentRegistry<ShipOverboardRider> _registry;
        private IShipClientBindingModel _shipClientBindingModel;

        public bool IsOverboard => _isOverboard;
        public PlayerKey Key => _rider.Key;
        public Vector3 Position => transform.position;
        internal ShipRider Rider => _rider;
        // Server: was in the safe space since the ship last took off or landed (in flight: rode the ship), so leaving it
        // is going overboard.
        internal bool HasBeenInside { get; set; }
        // Server: whether the ship flew on the last tick; a takeoff or a landing starts the safe space over.
        internal bool WasFlying { get; set; }
        internal float DriftSeconds => (float)(NetworkTime.time - _overboardSince);

        [Inject]
        private void InjectDependencies(
            ShipOverboardConfiguration shipOverboardConfiguration,
            IComponentRegistry<ShipOverboardRider> registry,
            IShipClientBindingModel shipClientBindingModel) {
            _shipOverboardConfiguration = shipOverboardConfiguration;
            _registry = registry;
            _shipClientBindingModel = shipClientBindingModel;
        }

        public override void OnStartServer() =>
            _registry.Add(this);

        public override void OnStopServer() =>
            _registry.Remove(this);

        public override void OnStartClient() {
            _registry.Add(this);
            ShowOverboard(_isOverboard);
        }

        public override void OnStopClient() {
            _registry.Remove(this);
            ShowOverboard(false);
        }

        private void FixedUpdate() {
            if (_isOverboard && _drift.IsOwnBody)
                TryCatchOnShip();
        }

        [Server]
        internal void ServerBeginOverboard() {
            _overboardSince = NetworkTime.time;
            _isOverboard = true;
            ShowOverboard(true);
        }

        [Server]
        internal void ServerEnd() {
            _isOverboard = false;
            ShowOverboard(false);
        }

        // Death or a leave: no drift, and the next one needs the player inside again first.
        [Server]
        internal void ServerReset() {
            HasBeenInside = false;
            if (_isOverboard)
                ServerEnd();
        }

        // Drifted too far from the ground's areas: the drift ends and the body is put at the pose on every peer.
        [Server]
        internal void ServerReturn(Pose pose) {
            ServerEnd();
            if (TryGetComponent(out IPlayerBodyPlacement placement) == false)
                throw new System.InvalidOperationException(name + " has no " + nameof(IPlayerBodyPlacement) + ".");

            placement.ServerPlace(pose);
        }

        // The owner's drifting body over the flying ship's deck rides the ship from the next step.
        private void TryCatchOnShip() {
            ShipBase ship = _shipClientBindingModel.Ship;
            if (ship == null || ship.IsFlying == false || DriftSeconds < _shipOverboardConfiguration.MinDriftSecondsBeforeCapture)
                return;

            ICarrier carrier = ship.Carrier;
            IMovableBody body = _drift.Body;
            ICarrierRider bodyRider = body.Rider;
            if (carrier == null || bodyRider == null || ReferenceEquals(bodyRider.Carrier, carrier))
                return;

            if (carrier.Holds(body.Position))
                bodyRider.CatchInAir(carrier);
        }

        private void ShowOverboard(bool isOverboard) {
            if (isOverboard)
                _drift.Begin(_shipOverboardConfiguration.Drift);
            else
                _drift.End();
        }

        private void OnOverboardChanged(bool previous, bool current) =>
            ShowOverboard(current);
    }
}
