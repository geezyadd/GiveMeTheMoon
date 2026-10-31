using System;
using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Server;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Spawn {
    // Finds one hidden point for the group and scatters the agents around it; a scattered point that is in a rock or
    // seen by a player falls back to the hidden point itself (the agents push apart as they fly off). No hidden point
    // at all spawns nothing. A group at a given point scatters the same way, falling back only out of a rock.
    public sealed class FieldAgentGroupSpawnService : IFieldAgentGroupSpawnService {
        private readonly IFieldAgentHiddenSpawnService _fieldAgentHiddenSpawnService;
        private readonly IFieldAgentViewpointSource _fieldAgentViewpointSource;
        private readonly IFieldAgentControl _fieldAgentControl;
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;

        public FieldAgentGroupSpawnService(
            IFieldAgentHiddenSpawnService fieldAgentHiddenSpawnService,
            IFieldAgentViewpointSource fieldAgentViewpointSource,
            IFieldAgentControl fieldAgentControl,
            FieldAgentConfiguration fieldAgentConfiguration) {
            _fieldAgentHiddenSpawnService = fieldAgentHiddenSpawnService;
            _fieldAgentViewpointSource = fieldAgentViewpointSource;
            _fieldAgentControl = fieldAgentControl;
            _fieldAgentConfiguration = fieldAgentConfiguration;
        }

        public FieldAgentSpawnCover SpawnHiddenGroup(FieldAgentProfile profile, int count, Vector3 center, int seed, Func<IFieldAgentBehaviour> createBehaviour, List<int> spawnedIds) {
            spawnedIds.Clear();
            List<Vector3> viewpoints = new();
            _fieldAgentViewpointSource.CollectViewpoints(viewpoints);
            if (_fieldAgentHiddenSpawnService.TryFindHiddenPoint(center, viewpoints, seed, out Vector3 point, out FieldAgentSpawnCover cover) == false)
                return FieldAgentSpawnCover.None;

            System.Random random = new(seed);
            for (int i = 0; i < count; i++)
                spawnedIds.Add(_fieldAgentControl.Spawn(profile, CreateScatteredPoint(point, viewpoints, random), createBehaviour()));

            return cover;
        }

        // Seen by the players on purpose (a nest's bugs fly out of it): only clear of rocks.
        public void SpawnGroupAt(FieldAgentProfile profile, int count, Vector3 point, int seed, Func<IFieldAgentBehaviour> createBehaviour, List<int> spawnedIds) {
            spawnedIds.Clear();
            System.Random random = new(seed);
            for (int i = 0; i < count; i++) {
                Vector3 scattered = Scatter(point, random);
                spawnedIds.Add(_fieldAgentControl.Spawn(profile, _fieldAgentHiddenSpawnService.IsClearOfRocks(scattered) ? scattered : point, createBehaviour()));
            }
        }

        private Vector3 CreateScatteredPoint(Vector3 point, IReadOnlyList<Vector3> viewpoints, System.Random random) {
            Vector3 scattered = Scatter(point, random);
            bool isUsable = _fieldAgentHiddenSpawnService.IsClearOfRocks(scattered) && _fieldAgentHiddenSpawnService.IsHidden(scattered, viewpoints);
            return isUsable ? scattered : point;
        }

        private Vector3 Scatter(Vector3 point, System.Random random) {
            Vector3 offset = new((float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f);
            return point + offset * _fieldAgentConfiguration.GroupScatterMeters;
        }
    }
}
