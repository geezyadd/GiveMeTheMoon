using System;

namespace Features.StatsModule.EntityStatsModule.Scripts.Modifier {
    public class StatModifier : IEquatable<StatModifier> {
        public ModifierType ModifierType { get; }
        public float Value { get; }
        public int Order { get; }

        public StatModifier(float value, ModifierType modifierType, int order) {
            Value = value;
            ModifierType = modifierType;
            Order = order;
        }
        
        public StatModifier(float value, ModifierType modifierType) : this (value, modifierType, (int)modifierType) { }

        public bool Equals(StatModifier other) {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return ModifierType == other.ModifierType && Value.Equals(other.Value) && Order == other.Order;
        }

        public override bool Equals(object obj) {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((StatModifier)obj);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = (int)ModifierType;
                hashCode = (hashCode * 397) ^ Value.GetHashCode();
                hashCode = (hashCode * 397) ^ Order;
                return hashCode;
            }
        }

        public static bool operator ==(StatModifier left, StatModifier right) =>
            Equals(left, right);

        public static bool operator !=(StatModifier left, StatModifier right) =>
            !Equals(left, right);
    }
}
