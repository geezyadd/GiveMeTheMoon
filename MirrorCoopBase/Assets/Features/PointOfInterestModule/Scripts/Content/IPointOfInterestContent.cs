using Features.PointOfInterestModule.Scripts.Network;

namespace Features.PointOfInterestModule.Scripts.Content {
    // Server: what one kind of content does when the ship enters its point's zone (bugs fly out, rocks fall). A new
    // content is a new class bound as one more IPointOfInterestContent and a content definition, never a branch.
    public interface IPointOfInterestContent {
        public string ContentId { get; }
        public void ServerEnter(in PointOfInterestEntry point);
    }
}
