using Features.WeaponModule.Contracts;

namespace Features.WeaponModule.Editor.Tests {
    public sealed class CountingWeaponTarget : IWeaponTarget {
        public bool IsAlive { get; set; } = true;
        public int Hits { get; private set; }
        public WeaponHit LastHit { get; private set; }

        public void ServerReceiveHit(in WeaponHit hit) {
            Hits++;
            LastHit = hit;
        }
    }
}
