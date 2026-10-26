using UnityEngine;

namespace Features.ShipDamageModule.Scripts.Damage {
    [CreateAssetMenu(
        fileName = nameof(ShipDamageConfiguration) + "_Default",
        menuName = "Configurations/ShipModule/" + nameof(ShipDamageConfiguration))]
    public sealed class ShipDamageConfiguration : ScriptableObject {
        [Tooltip("Only an installed module within this distance of a rock impact can break, in metres.")]
        [SerializeField, Min(0f)] private float _moduleSearchRadius = 6f;
        [Tooltip("Chance that an impact of full strength (1) breaks the nearest module; scaled by the impact strength and the module's multiplier.")]
        [SerializeField, Range(0f, 1f)] private float _moduleBreakChance = 0.5f;
        [Tooltip("Chance that an impact of full strength (1) opens a breach at the nearest free hull damage point; scaled by the impact strength.")]
        [SerializeField, Range(0f, 1f)] private float _breachChance = 0.6f;
        [Tooltip("Most breaches open at the same time; an impact opens no breach past this.")]
        [SerializeField, Min(0)] private int _maxOpenBreaches = 4;
        [Tooltip("Hull HP a repaired breach gives back, up to the hull max.")]
        [SerializeField, Min(0f)] private float _hullPerRepairedBreach = 10f;
        [Tooltip("Hull HP every open breach leaks per second in flight; 0 = no leak.")]
        [SerializeField, Min(0f)] private float _leakHullPerSecond = 0.5f;
        [Tooltip("The leak is dealt to the hull in steps of this many HP, so the hull bar flashes once per step.")]
        [SerializeField, Min(0.01f)] private float _leakStepHull = 1f;
        [Tooltip("Seconds of holding the interact button to repair with a tool of speed 1; a faster tool divides it.")]
        [SerializeField, Min(0.1f)] private float _repairSeconds = 2.5f;

        public float ModuleSearchRadius => _moduleSearchRadius;
        public float ModuleBreakChance => _moduleBreakChance;
        public float BreachChance => _breachChance;
        public int MaxOpenBreaches => _maxOpenBreaches;
        public float HullPerRepairedBreach => _hullPerRepairedBreach;
        public float LeakHullPerSecond => _leakHullPerSecond;
        public float LeakStepHull => _leakStepHull;
        public float RepairSeconds => _repairSeconds;
    }
}
