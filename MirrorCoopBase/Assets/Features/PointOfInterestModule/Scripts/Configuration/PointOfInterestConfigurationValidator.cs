using System;
using System.Collections.Generic;
using Zenject;

namespace Features.PointOfInterestModule.Scripts.Configuration {
    // The one startup check of the points of interest data: contents with unique ids, a look and a positive weight in
    // total, a sonar kind, and the bug nest's content among them.
    public sealed class PointOfInterestConfigurationValidator : IInitializable {
        private readonly PointOfInterestConfiguration _pointOfInterestConfiguration;
        private readonly BugNestConfiguration _bugNestConfiguration;

        public PointOfInterestConfigurationValidator(PointOfInterestConfiguration pointOfInterestConfiguration, BugNestConfiguration bugNestConfiguration) {
            _pointOfInterestConfiguration = pointOfInterestConfiguration;
            _bugNestConfiguration = bugNestConfiguration;
        }

        public void Initialize() {
            ValidateContents();
            if (_pointOfInterestConfiguration.SonarKind == null)
                throw new InvalidOperationException($"{_pointOfInterestConfiguration.name}: no sonar kind.");

            if (!Contains(_pointOfInterestConfiguration.Contents, _bugNestConfiguration.Content))
                throw new InvalidOperationException($"{_bugNestConfiguration.name}: its content is not in {_pointOfInterestConfiguration.name}.");
        }

        private void ValidateContents() {
            HashSet<string> ids = new();
            float totalWeight = 0f;
            foreach (PointOfInterestContentDefinition content in _pointOfInterestConfiguration.Contents) {
                if (content == null || string.IsNullOrEmpty(content.Id) || !ids.Add(content.Id))
                    throw new InvalidOperationException($"{_pointOfInterestConfiguration.name}: a content is empty, has no id or repeats one.");

                if (content.ViewPrefab == null)
                    throw new InvalidOperationException($"{content.name}: no view prefab.");

                totalWeight += content.Weight;
            }

            if (totalWeight <= 0f)
                throw new InvalidOperationException($"{_pointOfInterestConfiguration.name}: no content with a positive weight.");
        }

        private static bool Contains(IReadOnlyList<PointOfInterestContentDefinition> contents, PointOfInterestContentDefinition wanted) {
            foreach (PointOfInterestContentDefinition content in contents) {
                if (content == wanted)
                    return true;
            }

            return false;
        }
    }
}
