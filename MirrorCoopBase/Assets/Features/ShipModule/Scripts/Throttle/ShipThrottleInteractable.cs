using Features.CameraModule.Contracts;
using Features.GrabModule.Scripts;
using Features.GrabModule.Scripts.Held;
using Features.ShipModule.Scripts.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Throttle {
    // The throttle handle as a held control: hold the interact button on it to take it, move the mouse forward / back
    // to push / pull it, let go and it stays. One holder at a time by player key; a seated player cannot reach it.
    public sealed class ShipThrottleInteractable : InteractableBase, IHeldControl, IHeldControlDriver {
        [SerializeField] private ShipThrottleLever _shipThrottleLever;
        [SerializeField] private ShipBase _ship;

        private IReadOnlyShipThrottleModel _shipThrottleModel;
        private IReadOnlyShipRunModel _shipRunModel;
        private ShipThrottleConfiguration _shipThrottleConfiguration;
        private ICameraShakeService _cameraShakeService;
        private IShipThrottleLaunchRule _shipThrottleLaunchRule;
        private IShipThrottleDetentRule _shipThrottleDetentRule;
        private ShipThrottleLocalHold _localHold;

        public ShipLaunchReadiness LaunchReadiness => _ship.LaunchReadiness;

        [Inject]
        private void InjectDependencies(
            IReadOnlyShipThrottleModel shipThrottleModel,
            IReadOnlyShipRunModel shipRunModel,
            ShipThrottleConfiguration shipThrottleConfiguration,
            ICameraShakeService cameraShakeService,
            IShipThrottleLaunchRule shipThrottleLaunchRule,
            IShipThrottleDetentRule shipThrottleDetentRule) {
            _shipThrottleModel = shipThrottleModel;
            _shipRunModel = shipRunModel;
            _shipThrottleConfiguration = shipThrottleConfiguration;
            _cameraShakeService = cameraShakeService;
            _shipThrottleLaunchRule = shipThrottleLaunchRule;
            _shipThrottleDetentRule = shipThrottleDetentRule;
        }

        private void Start() =>
            _localHold = new ShipThrottleLocalHold(_shipThrottleConfiguration, new ShipThrottleHandle(_shipThrottleConfiguration, _shipThrottleDetentRule));

        public override bool CanUse(NetworkIdentity user, GrabController grab) {
            if (grab.IsHandFree == false || user.TryGetComponent(out ShipRider rider) == false || rider.IsSeated)
                return false;

            string holderKey = _shipThrottleModel.HolderKey;
            return string.IsNullOrEmpty(holderKey) || holderKey == rider.Key.Id;
        }

        public bool ServerTryTakeHold(NetworkIdentity user) =>
            _shipThrottleLever.ServerTryTakeHold(ReadPlayerKey(user));

        public void ServerLetGo(NetworkIdentity user) =>
            _shipThrottleLever.ServerLetGo(ReadPlayerKey(user));

        public void ServerReceivePosition(NetworkIdentity user, float position) =>
            _shipThrottleLever.ServerMoveByHolder(ReadPlayerKey(user), position);

        public void LocalBeginHold(NetworkIdentity user) =>
            _localHold.Begin(ReadPlayerKey(user), _shipThrottleModel.Position);

        public HeldControlFrame LocalDrive(Vector2 look, float deltaTime) {
            ShipThrottleLocalStep step = _localHold.Drive(look.y, deltaTime, ReadMaxPosition(), _shipThrottleModel.HolderKey);
            if (step.IsDetentClick)
                PlayDetentClick();

            return step.Frame;
        }

        public HeldControlFrame LocalEndHold() =>
            _localHold.End(ReadMaxPosition());

        // The handle as this peer moves it right now, while the local player holds it.
        public bool TryGetLocalPosition(out float position) {
            position = 0f;
            if (_localHold == null || _localHold.IsHolding == false)
                return false;

            position = _localHold.Position;
            return true;
        }

        private float ReadMaxPosition() =>
            _shipThrottleLaunchRule.ReadMaxPosition(_shipRunModel.Phase, _ship.CanLaunch);

        private void PlayDetentClick() =>
            _cameraShakeService.AddShake(_shipThrottleConfiguration.DetentClickShake, _shipThrottleConfiguration.DetentClickShakeStrength);

        private static string ReadPlayerKey(NetworkIdentity user) =>
            user.TryGetComponent(out ShipRider rider) ? rider.Key.Id : string.Empty;
    }
}
