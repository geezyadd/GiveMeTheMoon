using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The one startup check of the difficulty profiles: ids set and unique; debris kinds of the catalog with a recipe;
    // an obstacle density in 0..1 and debris size classes; giants of the catalog that are one rock
    // inside their footprint, and giant gap cells at least the ship gap wide.
    public sealed class FieldDifficultyProfileValidator : IInitializable {
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly FieldStructureCatalog _fieldStructureCatalog;

        public FieldDifficultyProfileValidator(FlightFieldConfiguration flightFieldConfiguration, FieldStructureCatalog fieldStructureCatalog) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldStructureCatalog = fieldStructureCatalog;
        }

        public void Initialize() {
            if (_flightFieldConfiguration.DifficultyProfiles.Count == 0)
                throw new InvalidOperationException($"{_flightFieldConfiguration.name}: no difficulty profile.");

            HashSet<string> ids = new(StringComparer.Ordinal);
            foreach (FieldDifficultyProfile profile in _flightFieldConfiguration.DifficultyProfiles)
                ValidateProfile(profile, ids);
        }

        private void ValidateProfile(FieldDifficultyProfile profile, HashSet<string> ids) {
            if (profile == null)
                throw new InvalidOperationException($"{_flightFieldConfiguration.name}: a difficulty profile entry is empty.");

            if (string.IsNullOrEmpty(profile.Id) || !ids.Add(profile.Id))
                throw new InvalidOperationException($"{profile.name}: the profile id is empty or used twice.");

            ValidateSizeClasses(profile);
            ValidateGiants(profile);
            ValidateDebris(profile);
        }

        private static void ValidateSizeClasses(FieldDifficultyProfile profile) {
            if (!IsShare(profile.ObstacleDensity))
                throw new InvalidOperationException($"{profile.name}: the obstacle density must be in 0..1.");

            float totalWeight = 0f;
            foreach (FieldRockSizeClass sizeClass in profile.DebrisSizeClasses) {
                if (!IsDiameterRange(sizeClass.DiameterMeters))
                    throw new InvalidOperationException($"{profile.name}: the diameters of the size class '{sizeClass.Name}' are empty or not a range.");

                totalWeight += sizeClass.Weight;
            }

            if (totalWeight <= 0f)
                throw new InvalidOperationException($"{profile.name}: an obstacle profile needs a debris size class with a positive weight.");
        }

        private void ValidateGiants(FieldDifficultyProfile profile) {
            if (profile.GiantGapCells * _flightFieldConfiguration.CellSize < _flightFieldConfiguration.DebrisMinGapMeters)
                throw new InvalidOperationException($"{profile.name}: the giant gap ({profile.GiantGapCells} cells) is narrower than the ship gap ({_flightFieldConfiguration.DebrisMinGapMeters} m).");

            foreach (FieldGiantClass giantClass in profile.GiantClasses) {
                foreach (FieldObstacleOption option in giantClass.Obstacles)
                    ValidateGiant(profile, giantClass, option);
            }
        }

        // One main rock and nothing else keeps every rock of the giant inside its footprint, at its middle; a giant
        // without a recipe is its prefab (a debug structure of one cell).
        private void ValidateGiant(FieldDifficultyProfile profile, FieldGiantClass giantClass, FieldObstacleOption option) {
            if (option.Structure == null || !_fieldStructureCatalog.TryGet(option.Structure.Id, out _))
                throw new InvalidOperationException($"{profile.name}: a giant of the class '{giantClass.Name}' is empty or missing from {_fieldStructureCatalog.name}.");

            if (!option.Structure.HasClusterRecipe)
                return;

            FieldClusterRecipe recipe = option.Structure.ClusterRecipe;
            bool isSingleRock = recipe.SatelliteCount.y == 0 && recipe.DebrisChance <= 0f;
            if (!isSingleRock)
                throw new InvalidOperationException($"{option.Structure.name}: a giant of an obstacle field must be one main rock (no satellites or debris).");
        }

        private void ValidateDebris(FieldDifficultyProfile profile) {
            float totalWeight = 0f;
            foreach (FieldDebrisOption option in profile.DebrisTypes) {
                if (option.Debris == null || option.Debris.Recipe == null || !IsInCatalog(option.Debris))
                    throw new InvalidOperationException($"{profile.name}: a debris kind is empty, has no recipe or is missing from {_fieldStructureCatalog.name}.");

                totalWeight += option.Weight;
            }

            if (totalWeight <= 0f)
                throw new InvalidOperationException($"{profile.name}: no debris kind with a positive weight.");
        }

        private bool IsInCatalog(FieldDebrisDefinition debris) {
            foreach (FieldDebrisDefinition candidate in _fieldStructureCatalog.Debris) {
                if (candidate == debris)
                    return true;
            }

            return false;
        }

        private static bool IsDiameterRange(Vector2 range) =>
            range.x > 0f && range.x <= range.y;

        private static bool IsShare(FieldDifficultyRange range) =>
            range.Easy >= 0f && range.Easy <= 1f && range.Hard >= 0f && range.Hard <= 1f;
    }
}
