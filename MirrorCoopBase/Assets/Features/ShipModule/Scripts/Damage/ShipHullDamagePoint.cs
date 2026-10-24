using UnityEngine;
using UnityEngine.Assertions;
using Zenject;

namespace Features.ShipModule.Scripts.Damage {
    // A place on the hull where an impact can open a breach; it shows the breach while it is open.
    public sealed class ShipHullDamagePoint : MonoBehaviour {
        [Tooltip("Stable id stored in the synced breach list: unique within the ship.")]
        [SerializeField] private string _pointId;
        [SerializeField] private ShipDamageVisual _visual;

        private IShipBreachCondition _shipBreachCondition;

        public string PointId => _pointId;
        public bool IsOpen => _shipBreachCondition.IsOpen(_pointId);

        [Inject]
        private void InjectDependencies(IShipBreachCondition shipBreachCondition) =>
            _shipBreachCondition = shipBreachCondition;

        private void Awake() =>
            Assert.IsFalse(string.IsNullOrEmpty(_pointId), name + " needs a damage point id unique within the ship.");

        private void OnEnable() {
            _shipBreachCondition.OnBreachesChanged += OnBreachesChanged;
            OnBreachesChanged();
        }

        private void OnDisable() =>
            _shipBreachCondition.OnBreachesChanged -= OnBreachesChanged;

        private void OnBreachesChanged() =>
            _visual.Show(IsOpen);
    }
}
