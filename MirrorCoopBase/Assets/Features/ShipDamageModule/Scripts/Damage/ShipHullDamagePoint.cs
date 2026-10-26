using Features.ShipModule.Scripts.Contracts;
using UnityEngine;
using UnityEngine.Assertions;
using Zenject;

namespace Features.ShipDamageModule.Scripts.Damage {
    // A place on the hull where an impact can open a breach; it shows the breach while it is open.
    public sealed class ShipHullDamagePoint : MonoBehaviour {
        [Tooltip("Stable id stored in the synced breach list: unique within the ship.")]
        [SerializeField] private string _pointId;
        [SerializeField] private ShipDamageVisual _visual;

        private IShipBreachCondition _shipBreachCondition;
        private IComponentRegistry<ShipHullDamagePoint> _shipHullDamagePointRegistry;

        public string PointId => _pointId;
        public bool IsOpen => _shipBreachCondition.IsOpen(_pointId);

        [Inject]
        private void InjectDependencies(IShipBreachCondition shipBreachCondition, IComponentRegistry<ShipHullDamagePoint> shipHullDamagePointRegistry) {
            _shipBreachCondition = shipBreachCondition;
            _shipHullDamagePointRegistry = shipHullDamagePointRegistry;
        }

        private void Awake() =>
            Assert.IsFalse(string.IsNullOrEmpty(_pointId), name + " needs a damage point id unique within the ship.");

        private void OnEnable() {
            _shipHullDamagePointRegistry.Add(this);
            _shipBreachCondition.OnBreachesChanged += OnBreachesChanged;
            OnBreachesChanged();
        }

        private void OnDisable() {
            _shipHullDamagePointRegistry.Remove(this);
            _shipBreachCondition.OnBreachesChanged -= OnBreachesChanged;
        }

        private void OnBreachesChanged() =>
            _visual.Show(IsOpen);
    }
}
