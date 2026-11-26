using Features.TargetModule;
using UnityEngine;
using Zenject;

namespace Features.AttackModule {
    // An attack's aim over the TargetRegistry (IAttackAim): the nearest available Hittable target in front of the
    // attacker, within the attack's distance and angle from the aim origin, the facing's forward on the ground plane,
    // other than the attacker's own target.
    public sealed class TargetAttackAim : MonoBehaviour, IAttackAim {
        [Tooltip("Where the distance is measured from (the middle of the attacker's head).")]
        [SerializeField] private Transform _origin;
        [Tooltip("Whose forward is the attacker's facing (a part turned to where the attacker looks).")]
        [SerializeField] private Transform _facing;
        [Tooltip("The attacker's own target, never aimed at. Empty when it has none.")]
        [SerializeField] private Target _own;

        private TargetRegistry _registry;

        [Inject]
        private void Construct(TargetRegistry registry) =>
            _registry = registry;

        public Target PickTarget(AttackDefinition attack) =>
            _registry?.PickNearest(TargetKind.Hittable, _origin.position, _facing.forward, attack.MaxDistance,
                attack.MaxAngle, _own);
    }
}
