using System.Collections.Generic;
using Features.CutsceneModule.Contracts;
using Features.FieldAgentModule.Scripts.Client;
using Features.PointOfInterestModule.Scripts.Configuration;
using Features.PointOfInterestModule.Scripts.Network;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Content {
    // Every peer: a nest's flock is the middle of the agents this peer shows near the nest, so each player's camera
    // follows the bugs it sees. No agent near it yet: no target (the shot looks at the nest).
    public sealed class BugNestFlockTargetSource : ICutsceneTargetSource {
        private readonly IBugNestFlockTargetIds _bugNestFlockTargetIds;
        private readonly IPointsOfInterest _pointsOfInterest;
        private readonly IFieldAgentShownAgents _fieldAgentShownAgents;
        private readonly BugNestConfiguration _bugNestConfiguration;
        private readonly List<int> _shownIds = new();

        public BugNestFlockTargetSource(IBugNestFlockTargetIds bugNestFlockTargetIds, IPointsOfInterest pointsOfInterest, IFieldAgentShownAgents fieldAgentShownAgents,
                                        BugNestConfiguration bugNestConfiguration) {
            _bugNestFlockTargetIds = bugNestFlockTargetIds;
            _pointsOfInterest = pointsOfInterest;
            _fieldAgentShownAgents = fieldAgentShownAgents;
            _bugNestConfiguration = bugNestConfiguration;
        }

        public bool TryGetTargetPosition(string targetId, out Vector3 position) {
            position = Vector3.zero;
            if (!_bugNestFlockTargetIds.TryGetPointId(targetId, out int pointId) || !_pointsOfInterest.TryFindIndex(pointId, out int index))
                return false;

            return TryGetFlockMiddle(_pointsOfInterest.Get(index).Position, out position);
        }

        private bool TryGetFlockMiddle(Vector3 nest, out Vector3 middle) {
            middle = Vector3.zero;
            int count = 0;
            _fieldAgentShownAgents.CollectShownIds(_shownIds);
            foreach (int id in _shownIds) {
                if (!_fieldAgentShownAgents.TryGetShownWorldPosition(id, out Vector3 agent) || Vector3.Distance(agent, nest) > _bugNestConfiguration.FlockSearchMeters)
                    continue;

                middle += agent;
                count++;
            }

            if (count == 0)
                return false;

            middle /= count;
            return true;
        }
    }
}
