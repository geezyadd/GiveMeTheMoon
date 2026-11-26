using Features.TargetModule;

namespace Features.AttackModule {
    // Picks what an attack goes for when it starts, on every peer that simulates the attacker.
    public interface IAttackAim {
        // The target the limb reaches for while the attack lasts (its aim point); null: nothing in reach, the attack
        // is the plain animation.
        Target PickTarget(AttackDefinition attack);
    }
}
