using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Damage {
    // A socket's broken module smokes and sparks on every peer until it is repaired.
    public sealed class ShipSocketDamageView : MonoBehaviour {
        [SerializeField] private ShipSocket _socket;
        [SerializeField] private ShipDamageVisual _visual;

        private IShipModuleCondition _shipModuleCondition;

        [Inject]
        private void InjectDependencies(IShipModuleCondition shipModuleCondition) =>
            _shipModuleCondition = shipModuleCondition;

        private void OnEnable() {
            _shipModuleCondition.OnConditionChanged += OnConditionChanged;
            OnConditionChanged();
        }

        private void OnDisable() =>
            _shipModuleCondition.OnConditionChanged -= OnConditionChanged;

        private void OnConditionChanged() =>
            _visual.Show(_shipModuleCondition.IsWorking(_socket.SocketId) == false);
    }
}
