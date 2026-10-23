using UnityEngine;

namespace Features.ShipModule.Scripts.Hull {
    [CreateAssetMenu(
        fileName = nameof(ShipHullConfiguration) + "_Default",
        menuName = "Configurations/ShipModule/" + nameof(ShipHullConfiguration))]
    public sealed class ShipHullConfiguration : ScriptableObject {
        [Tooltip("Share of incoming damage one point of the Armor stat takes off: 0.01 = 1 % per point.")]
        [SerializeField] private float _damageReductionPerArmor = 0.01f;
        [Tooltip("The largest share of incoming damage armor can take off, from 0 up to (not including) 1.")]
        [SerializeField] private float _maxArmorReduction = 0.75f;
        [Tooltip("Repair the hull to full on every landing at a station.")]
        [SerializeField] private bool _repairOnLanding = true;
        [Tooltip("Editor / development builds: the damage of one debug hit (F7).")]
        [SerializeField] private float _debugDamage = 10f;

        public float DamageReductionPerArmor => _damageReductionPerArmor;
        public float MaxArmorReduction => _maxArmorReduction;
        public bool RepairOnLanding => _repairOnLanding;
        public float DebugDamage => _debugDamage;
    }
}
