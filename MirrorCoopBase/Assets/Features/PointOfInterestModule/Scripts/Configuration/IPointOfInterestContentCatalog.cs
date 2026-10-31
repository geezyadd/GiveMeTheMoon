namespace Features.PointOfInterestModule.Scripts.Configuration {
    public interface IPointOfInterestContentCatalog {
        // The content of the configuration with this id; a point names only contents the server picked from it.
        public PointOfInterestContentDefinition GetRequired(string contentId);
        // A content by weight for a roll in 0..1.
        public PointOfInterestContentDefinition Pick(float roll);
    }
}
