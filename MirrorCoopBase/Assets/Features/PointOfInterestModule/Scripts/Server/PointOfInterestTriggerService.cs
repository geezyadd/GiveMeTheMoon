using System.Collections.Generic;
using Features.PointOfInterestModule.Scripts.Content;
using Features.PointOfInterestModule.Scripts.Network;

namespace Features.PointOfInterestModule.Scripts.Server {
    public sealed class PointOfInterestTriggerService : IPointOfInterestTriggerService {
        private readonly IPointsOfInterest _pointsOfInterest;
        private readonly IPointOfInterestWriter _pointOfInterestWriter;
        private readonly List<IPointOfInterestContent> _pointOfInterestContents;

        public PointOfInterestTriggerService(IPointsOfInterest pointsOfInterest, IPointOfInterestWriter pointOfInterestWriter, List<IPointOfInterestContent> pointOfInterestContents) {
            _pointsOfInterest = pointsOfInterest;
            _pointOfInterestWriter = pointOfInterestWriter;
            _pointOfInterestContents = pointOfInterestContents;
        }

        public PointOfInterestTriggerResult ServerTrigger(int pointId) {
            if (!_pointsOfInterest.TryFindIndex(pointId, out int index))
                return PointOfInterestTriggerResult.NotFound;

            PointOfInterestEntry point = _pointsOfInterest.Get(index);
            if (point.State != PointOfInterestState.Dormant)
                return PointOfInterestTriggerResult.AlreadyTriggered;

            point.State = PointOfInterestState.Triggered;
            _pointOfInterestWriter.ServerSet(index, point);
            GetContent(point.ContentId).ServerEnter(point);
            return PointOfInterestTriggerResult.Triggered;
        }

        private IPointOfInterestContent GetContent(string contentId) {
            foreach (IPointOfInterestContent content in _pointOfInterestContents) {
                if (content.ContentId == contentId)
                    return content;
            }

            throw new KeyNotFoundException($"No point of interest content class for '{contentId}'.");
        }
    }
}
