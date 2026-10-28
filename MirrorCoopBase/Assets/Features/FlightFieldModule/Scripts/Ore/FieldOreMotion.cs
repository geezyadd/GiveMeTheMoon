using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Network;
using Features.GameCoreModule.Contracts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    // A drifting piece moves in field space, so a world shift moves it with the field. A hooked piece keeps its offset
    // from the ship's middle (world axes, so the ship's turns do not swing it) and the offset shrinks at the pull speed
    // towards a point above the ship: it rides with the ship and comes in at a constant speed whatever the ship does.
    // Each peer uses its own ship, so a client sees the piece arrive at the ship it shows.
    public sealed class FieldOreMotion : IFieldOreMotion {
        private readonly IFieldOres _fieldOres;
        private readonly IShipPositionSource _shipPositionSource;
        private readonly FieldOreConfiguration _fieldOreConfiguration;

        private Vector3 PullTargetOffset => Vector3.up * _fieldOreConfiguration.PullTargetHeightMeters;

        public FieldOreMotion(IFieldOres fieldOres, IShipPositionSource shipPositionSource, FieldOreConfiguration fieldOreConfiguration) {
            _fieldOres = fieldOres;
            _shipPositionSource = shipPositionSource;
            _fieldOreConfiguration = fieldOreConfiguration;
        }

        public Vector3 GetWorldPosition(in FieldOreEntry entry, double now) {
            float elapsed = GetElapsedSeconds(entry, now);
            if (IsHooked(entry) && _shipPositionSource.TryGetShipPosition(out Vector3 shipPosition))
                return shipPosition + Vector3.MoveTowards(entry.ShipOffset, PullTargetOffset, entry.PullSpeed * elapsed);

            Vector3 fieldPosition = entry.FieldPosition + entry.FieldVelocity * (IsHooked(entry) ? 0f : elapsed);
            return _fieldOres.Origin + _fieldOres.Rotation * fieldPosition;
        }

        public bool IsPullComplete(in FieldOreEntry entry, double now) =>
            IsHooked(entry) && entry.PullSpeed * GetElapsedSeconds(entry, now) >= Vector3.Distance(entry.ShipOffset, PullTargetOffset);

        public Vector3 ToFieldPosition(Vector3 worldPosition) =>
            Quaternion.Inverse(_fieldOres.Rotation) * (worldPosition - _fieldOres.Origin);

        public Vector3 ToFieldDirection(Vector3 worldDirection) =>
            Quaternion.Inverse(_fieldOres.Rotation) * worldDirection;

        public bool TryGetShipOffset(Vector3 worldPosition, out Vector3 shipOffset) {
            shipOffset = Vector3.zero;
            if (!_shipPositionSource.TryGetShipPosition(out Vector3 shipPosition))
                return false;

            shipOffset = worldPosition - shipPosition;
            return true;
        }

        private static bool IsHooked(in FieldOreEntry entry) =>
            !string.IsNullOrEmpty(entry.HookerId);

        private static float GetElapsedSeconds(in FieldOreEntry entry, double now) =>
            Mathf.Max(0f, (float)(now - entry.StartTime));
    }
}
