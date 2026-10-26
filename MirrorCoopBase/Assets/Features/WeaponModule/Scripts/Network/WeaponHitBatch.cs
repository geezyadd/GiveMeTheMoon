using System;

namespace Features.WeaponModule.Scripts.Network {
    // The hits one shooter sends in one network tick. Fixed slots instead of an array, so that neither sending nor
    // receiving a batch allocates during continuous fire; a minigun lands about one hit per tick.
    public struct WeaponHitBatch {
        public const int CAPACITY = 4;
        private const string SLOT_OUT_OF_RANGE_FORMAT = "Hit batch slot {0} is outside 0..{1}.";

        // The weapon's index in the weapon catalog: the server checks that the shooter holds this weapon.
        public int WeaponIndex;
        public int Count;
        public WeaponHitReport Hit0;
        public WeaponHitReport Hit1;
        public WeaponHitReport Hit2;
        public WeaponHitReport Hit3;

        public bool IsFull => Count >= CAPACITY;

        public void Add(in WeaponHitReport hit) {
            SetSlot(Count, hit);
            Count++;
        }

        public WeaponHitReport Get(int slot) {
            switch (slot) {
                case 0:
                    return Hit0;
                case 1:
                    return Hit1;
                case 2:
                    return Hit2;
                case 3:
                    return Hit3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot), string.Format(SLOT_OUT_OF_RANGE_FORMAT, slot, CAPACITY - 1));
            }
        }

        private void SetSlot(int slot, in WeaponHitReport hit) {
            switch (slot) {
                case 0:
                    Hit0 = hit;
                    break;
                case 1:
                    Hit1 = hit;
                    break;
                case 2:
                    Hit2 = hit;
                    break;
                case 3:
                    Hit3 = hit;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot), string.Format(SLOT_OUT_OF_RANGE_FORMAT, slot, CAPACITY - 1));
            }
        }
    }
}
