namespace Features.ShipModule.Scripts.Overboard {
    public interface IShipOverboardWeaponIds {
        public ulong GetTargetId(uint playerNetId);
        public bool TryGetPlayerNetId(ulong targetId, out uint playerNetId);
    }
}
