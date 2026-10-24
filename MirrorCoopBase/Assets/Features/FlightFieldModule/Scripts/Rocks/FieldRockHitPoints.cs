using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // The hit points of a breakable rock from the data: the configured curve at the rock's diameter. A smooth curve
    // may dip between its keys, so a rock never has less than one hit point.
    public sealed class FieldRockHitPoints : IFieldRockHitPoints {
        private const float MIN_HIT_POINTS = 1f;

        private readonly FieldRockDurabilityConfiguration _fieldRockDurabilityConfiguration;

        public FieldRockHitPoints(FieldRockDurabilityConfiguration fieldRockDurabilityConfiguration) =>
            _fieldRockDurabilityConfiguration = fieldRockDurabilityConfiguration;

        public float GetHitPoints(in FieldRockDescription rock) =>
            Mathf.Max(MIN_HIT_POINTS, _fieldRockDurabilityConfiguration.HitPointsByDiameter.Evaluate(rock.Diameter));
    }
}
