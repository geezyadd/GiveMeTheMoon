namespace Features.PointOfInterestModule.Scripts.Network {
    // The points of interest of the live field as every peer reads them.
    public interface IPointsOfInterest {
        public int Count { get; }
        public PointOfInterestEntry Get(int index);
        public bool TryFindIndex(int id, out int index);
    }
}
