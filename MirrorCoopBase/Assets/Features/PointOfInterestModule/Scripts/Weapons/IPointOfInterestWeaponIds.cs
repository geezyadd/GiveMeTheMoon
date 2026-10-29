namespace Features.PointOfInterestModule.Scripts.Weapons {
    public interface IPointOfInterestWeaponIds {
        public ulong GetTargetId(int pointId);
        public bool TryGetPointId(ulong targetId, out int pointId);
    }
}
