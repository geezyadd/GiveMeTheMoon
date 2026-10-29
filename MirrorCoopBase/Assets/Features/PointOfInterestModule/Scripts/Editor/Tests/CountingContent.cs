using Features.PointOfInterestModule.Scripts.Content;
using Features.PointOfInterestModule.Scripts.Network;

namespace Features.PointOfInterestModule.Scripts.Editor.Tests {
    public sealed class CountingContent : IPointOfInterestContent {
        public string ContentId { get; }
        public int Entries { get; private set; }

        public CountingContent(string contentId) =>
            ContentId = contentId;

        public void ServerEnter(in PointOfInterestEntry point) =>
            Entries++;
    }
}
