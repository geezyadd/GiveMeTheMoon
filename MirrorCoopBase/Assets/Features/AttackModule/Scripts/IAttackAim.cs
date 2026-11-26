using UnityEngine;

namespace Features.AttackModule {
    // Picks what an attack goes for when it starts, on every peer that simulates the attacker. The attacker's own kind
    // of body knows where its eyes look and what may be hit.
    public interface IAttackAim {
        // The point the limb reaches for (it follows the transform while the attack lasts); null: nothing in reach,
        // the attack is the plain animation.
        Transform PickTarget(AttackDefinition attack);
    }
}
