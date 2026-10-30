namespace Features.ParcelModule.Scripts.Services {
    public interface IParcelWeaponIds {
        public ulong GetTargetId(uint crateNetId);
        public bool TryGetCrateNetId(ulong targetId, out uint crateNetId);
    }
}
