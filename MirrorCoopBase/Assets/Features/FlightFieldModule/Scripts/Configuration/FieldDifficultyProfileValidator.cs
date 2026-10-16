using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Structures;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // The one startup check of the difficulty profiles: ids set and unique; shares and thresholds in 0..1 with room
    // left for empty cells; corridors at least as wide as the ship's passage; a wall; wall and ridge options of the
    // catalog with a single-cell option each (so every closed cell gets a rock); debris kinds of the catalog.
    public sealed class FieldDifficultyProfileValidator : IInitializable {
        private const int SINGLE_CELL = 1;

        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldFootprintReader _fieldFootprintReader;

        public FieldDifficultyProfileValidator(FlightFieldConfiguration flightFieldConfiguration, FieldStructureCatalog fieldStructureCatalog,
                                               IFieldFootprintReader fieldFootprintReader) {
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldStructureCatalog = fieldStructureCatalog;
            _fieldFootprintReader = fieldFootprintReader;
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

            ValidateShares(profile);
            ValidateWidths(profile);
            ValidateObstacles(profile, profile.WallObstacles, "wall");
            ValidateObstacles(profile, profile.RidgeObstacles, "ridge");
            ValidateDebris(profile);
        }

        private static void ValidateShares(FieldDifficultyProfile profile) {
            bool isZoneShareValid = IsShare(profile.BlockedShare) && IsShare(profile.DebrisShare)
                && profile.BlockedShare.Easy + profile.DebrisShare.Easy <= 1f && profile.BlockedShare.Hard + profile.DebrisShare.Hard <= 1f;
            if (!isZoneShareValid)
                throw new InvalidOperationException($"{profile.name}: the blocked and debris shares must be in 0..1 and add up to at most 1.");

            bool isThresholdValid = IsShare(profile.RidgeThreshold) && IsShare(profile.GladeThreshold) && IsShare(profile.CorridorDebrisDensity)
                && IsShare(profile.AlternativeDebrisDensity) && IsShare(profile.GladeDebrisDensity) && IsShare(profile.AlternativeClosedChance);
            if (!isThresholdValid)
                throw new InvalidOperationException($"{profile.name}: thresholds, densities and chances must be in 0..1.");
        }

        private void ValidateWidths(FieldDifficultyProfile profile) {
            int clearance = _flightFieldConfiguration.PassageClearanceCells;
            bool isWideEnough = IsAtLeast(profile.CorridorHalfWidthCells, clearance) && IsAtLeast(profile.AlternativeHalfWidthCells, clearance)
                && IsAtLeast(profile.DeadEndHalfWidthCells, clearance);
            if (!isWideEnough)
                throw new InvalidOperationException($"{profile.name}: a corridor half width is below the passage clearance ({clearance} cells): the ship would not fit.");

            if (profile.WallThicknessCells < 1)
                throw new InvalidOperationException($"{profile.name}: the wall is thinner than one cell.");
        }

        private void ValidateObstacles(FieldDifficultyProfile profile, IReadOnlyList<FieldObstacleOption> options, string zone) {
            bool hasSingleCell = false;
            foreach (FieldObstacleOption option in options) {
                if (option.Structure == null || !_fieldStructureCatalog.TryGet(option.Structure.Id, out _))
                    throw new InvalidOperationException($"{profile.name}: a {zone} obstacle is empty or missing from {_fieldStructureCatalog.name}.");

                hasSingleCell |= option.Weight > 0f && _fieldFootprintReader.Read(option.Structure, FieldRotation.None).Cells.Count == SINGLE_CELL;
            }

            if (!hasSingleCell)
                throw new InvalidOperationException($"{profile.name}: no {zone} obstacle with a positive weight takes a single cell: the {zone} would get holes.");
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

        private static bool IsShare(FieldDifficultyRange range) =>
            range.Easy >= 0f && range.Easy <= 1f && range.Hard >= 0f && range.Hard <= 1f;

        private static bool IsAtLeast(FieldDifficultyRange range, int least) =>
            Math.Round(range.Easy) >= least && Math.Round(range.Hard) >= least;
    }
}
