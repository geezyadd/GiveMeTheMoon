using System;
using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FlightFieldModule.Contracts;
using Features.SpaceHazeModule.Scripts.Configurations;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Spawn {
    // Seeded candidates on a shell around the centre, clear of every rock. First choice: a point a rock sphere hides
    // from every player view (the line from the view to the point crosses the sphere). Else a point on a shell past
    // the haze, where every view either has the point past the haze's hidden distance or behind a rock.
    public sealed class FieldAgentHiddenSpawnService : IFieldAgentHiddenSpawnService {
        private const float MIN_SEGMENT = 0.01f;

        private readonly IFieldRockQuery _fieldRockQuery;
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;
        private readonly SpaceHazeConfiguration _spaceHazeConfiguration;

        public FieldAgentHiddenSpawnService(IFieldRockQuery fieldRockQuery, FieldAgentConfiguration fieldAgentConfiguration, SpaceHazeConfiguration spaceHazeConfiguration) {
            _fieldRockQuery = fieldRockQuery;
            _fieldAgentConfiguration = fieldAgentConfiguration;
            _spaceHazeConfiguration = spaceHazeConfiguration;
        }

        public bool TryFindHiddenPoint(Vector3 center, IReadOnlyList<Vector3> viewpoints, int seed, out Vector3 point, out FieldAgentSpawnCover cover) {
            System.Random random = new(seed);
            cover = FieldAgentSpawnCover.Rock;
            if (TryFindOnShell(center, _fieldAgentConfiguration.SpawnMinMeters, _fieldAgentConfiguration.SpawnMaxMeters, random, viewpoints, IsHiddenByRock, out point))
                return true;

            cover = FieldAgentSpawnCover.Haze;
            float hazeShell = _spaceHazeConfiguration.HiddenMeters + _fieldAgentConfiguration.SpawnHazeMarginMeters + CalculateViewSpread(center, viewpoints);
            if (TryFindOnShell(center, hazeShell, hazeShell + _fieldAgentConfiguration.SpawnHazeMarginMeters, random, viewpoints, IsHidden, out point))
                return true;

            cover = FieldAgentSpawnCover.None;
            return false;
        }

        public bool IsHiddenByRock(Vector3 point, IReadOnlyList<Vector3> viewpoints) {
            for (int i = 0; i < viewpoints.Count; i++) {
                if (IsBehindRock(viewpoints[i], point) == false)
                    return false;
            }

            return true;
        }

        public bool IsHidden(Vector3 point, IReadOnlyList<Vector3> viewpoints) {
            float hidden = _spaceHazeConfiguration.HiddenMeters;
            for (int i = 0; i < viewpoints.Count; i++) {
                bool isPastHaze = Vector3.Distance(viewpoints[i], point) >= hidden;
                if (isPastHaze == false && IsBehindRock(viewpoints[i], point) == false)
                    return false;
            }

            return true;
        }

        // Spawning is a cold path: each query fills a list of its own.
        public bool IsClearOfRocks(Vector3 point) {
            List<FieldRockDescription> rocks = new();
            _fieldRockQuery.FindInSphere(point, _fieldAgentConfiguration.SpawnRockClearance, rocks);
            return rocks.Count == 0;
        }

        private bool TryFindOnShell(Vector3 center, float minMeters, float maxMeters, System.Random random, IReadOnlyList<Vector3> viewpoints, Func<Vector3, IReadOnlyList<Vector3>, bool> isHidden, out Vector3 point) {
            for (int attempt = 0; attempt < _fieldAgentConfiguration.SpawnTries; attempt++) {
                point = CreateCandidate(center, minMeters, maxMeters, random);
                if (IsClearOfRocks(point) && isHidden(point, viewpoints))
                    return true;
            }

            point = center;
            return false;
        }

        private Vector3 CreateCandidate(Vector3 center, float minMeters, float maxMeters, System.Random random) {
            float angle = (float)(random.NextDouble() * Mathf.PI * 2d);
            float distance = Mathf.Lerp(minMeters, maxMeters, (float)random.NextDouble());
            float height = ((float)random.NextDouble() * 2f - 1f) * _fieldAgentConfiguration.SpawnHeightSpreadMeters;
            return center + new Vector3(Mathf.Cos(angle) * distance, height, Mathf.Sin(angle) * distance);
        }

        // A sphere crossing the view line between its ends hides the point; one around either end does not count.
        private bool IsBehindRock(Vector3 viewpoint, Vector3 point) {
            Vector3 segment = point - viewpoint;
            float length = segment.magnitude;
            if (length < MIN_SEGMENT)
                return false;

            List<FieldRockDescription> rocks = new();
            _fieldRockQuery.FindInSphere(viewpoint + segment * 0.5f, length * 0.5f, rocks);
            for (int i = 0; i < rocks.Count; i++) {
                if (IsCrossing(viewpoint, point, segment / length, length, rocks[i]))
                    return true;
            }

            return false;
        }

        private static bool IsCrossing(Vector3 viewpoint, Vector3 point, Vector3 direction, float length, FieldRockDescription rock) {
            float radius = rock.Radius;
            if ((rock.Center - viewpoint).sqrMagnitude <= radius * radius || (rock.Center - point).sqrMagnitude <= radius * radius)
                return false;

            float along = Mathf.Clamp(Vector3.Dot(rock.Center - viewpoint, direction), 0f, length);
            Vector3 closest = viewpoint + direction * along;
            return (rock.Center - closest).sqrMagnitude < radius * radius;
        }

        private static float CalculateViewSpread(Vector3 center, IReadOnlyList<Vector3> viewpoints) {
            float spread = 0f;
            for (int i = 0; i < viewpoints.Count; i++)
                spread = Mathf.Max(spread, Vector3.Distance(center, viewpoints[i]));

            return spread;
        }
    }
}
