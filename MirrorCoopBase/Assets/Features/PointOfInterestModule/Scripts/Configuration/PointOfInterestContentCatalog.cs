using System;
using System.Collections.Generic;

namespace Features.PointOfInterestModule.Scripts.Configuration {
    // The configuration's contents by id and by weight; a handful of entries, so every call walks them.
    public sealed class PointOfInterestContentCatalog : IPointOfInterestContentCatalog {
        private readonly PointOfInterestConfiguration _pointOfInterestConfiguration;

        public PointOfInterestContentCatalog(PointOfInterestConfiguration pointOfInterestConfiguration) =>
            _pointOfInterestConfiguration = pointOfInterestConfiguration;

        public PointOfInterestContentDefinition GetRequired(string contentId) {
            foreach (PointOfInterestContentDefinition content in _pointOfInterestConfiguration.Contents) {
                if (content.Id == contentId)
                    return content;
            }

            throw new KeyNotFoundException($"{_pointOfInterestConfiguration.name} has no point of interest content '{contentId}'.");
        }

        public PointOfInterestContentDefinition Pick(float roll) {
            IReadOnlyList<PointOfInterestContentDefinition> contents = _pointOfInterestConfiguration.Contents;
            float target = roll * TotalWeight(contents);
            for (int i = 0; i < contents.Count; i++) {
                target -= contents[i].Weight;
                if (target < 0f)
                    return contents[i];
            }

            return contents[contents.Count - 1];
        }

        private static float TotalWeight(IReadOnlyList<PointOfInterestContentDefinition> contents) {
            float total = 0f;
            foreach (PointOfInterestContentDefinition content in contents)
                total += content.Weight;

            return total;
        }
    }
}
