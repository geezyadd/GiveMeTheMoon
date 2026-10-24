using Features.CharacterMovableModule.Scripts;
using Features.NetworkModelModule.Scripts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Helm {
    // The helm's slot view as a seat station. On the seated owner's peer the move stick's sideways axis (A / D) steers:
    // the host's flight reads it at once, a client sends it to the server through its rider (HelmSteer decides when).
    public sealed class ShipHelmStation : MonoBehaviour, ISeatStation, ISeatStationOwnerInput, IShipSteeringStation {
        private readonly HelmSteer _steer = new HelmSteer();

        private CharacterInputBuffer _characterInputBuffer;
        private ShipFlightSettings _shipFlightSettings;

        private ShipBase _ship;
        private ShipRider _occupantRider;

        public bool HasPilot => _steer.HasPilot;
        public bool IsSteeredByLocalPlayer => _steer.IsOccupantLocal;

        [Inject]
        private void InjectDependencies(CharacterInputBuffer characterInputBuffer, ShipFlightSettings shipFlightSettings) {
            _characterInputBuffer = characterInputBuffer;
            _shipFlightSettings = shipFlightSettings;
        }

        private void Awake() =>
            _ship = GetComponentInParent<ShipBase>();

        private void OnDestroy() =>
            Exit();

        private void Update() {
            if (_steer.HasPilot && _occupantRider == null)
                TryTakeOccupant();

            if (_steer.TryTakeSendTurn(NetworkServer.active, Time.unscaledTimeAsDouble, _shipFlightSettings.HelmSteerSendSeconds))
                _occupantRider.OwnerSendSeatInput(OwnerStick());
        }

        public void Enter(PlayerKey occupant) {
            Exit();
            _steer.Enter(occupant);
            TryTakeOccupant();
        }

        public void Exit() {
            _steer.Exit();
            _occupantRider = null;
        }

        public void ServerReceiveOwnerInput(float value) =>
            _steer.ServerReceive(value);

        public float ReadSteer() =>
            _steer.Read(OwnerStick());

        // The pilot's player object may spawn on this peer after the seat state (a late join), or be gone with its
        // player: taken when it is there.
        private void TryTakeOccupant() {
            bool isFound = _ship.Riders.TryFindByKey(_steer.Occupant, out _occupantRider);
            _steer.TakeOccupant(isFound && _occupantRider.isOwned);
        }

        private float OwnerStick() =>
            _characterInputBuffer.MoveStick.x;
    }
}
