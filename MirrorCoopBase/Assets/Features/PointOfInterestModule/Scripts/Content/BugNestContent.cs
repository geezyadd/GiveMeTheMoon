using System;
using System.Collections.Generic;
using Features.BugModule.Scripts.Spawn;
using Features.CutsceneModule.Contracts;
using Features.GameCoreModule.Contracts;
using Features.PointOfInterestModule.Scripts.Configuration;
using Features.PointOfInterestModule.Scripts.Network;
using Features.ShipModule.Scripts.Throttle;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Content {
    // Variant D, the bug nest: when the ship enters the zone a group of bugs comes out from behind the reward container
    // (seen from the ship) and attacks as every bug group does. With the entry cutscene on, the ship's throttle goes to
    // Stop first and every player watches the nest and the flock for a few seconds.
    public sealed class BugNestContent : IPointOfInterestContent {
        private const float DIRECTION_EPSILON = 0.0001f;

        private readonly IBugSpawnService _bugSpawnService;
        private readonly IShipThrottleStop _shipThrottleStop;
        private readonly ICutsceneDirector _cutsceneDirector;
        private readonly IShipPositionSource _shipPositionSource;
        private readonly IBugNestCutsceneShots _bugNestCutsceneShots;
        private readonly IBugNestFlockTargetIds _bugNestFlockTargetIds;
        private readonly BugNestConfiguration _bugNestConfiguration;
        private readonly List<int> _spawnedIds = new();
        private readonly List<CutsceneShot> _shots = new();

        public string ContentId => _bugNestConfiguration.Content.Id;

        public BugNestContent(
            IBugSpawnService bugSpawnService,
            IShipThrottleStop shipThrottleStop,
            ICutsceneDirector cutsceneDirector,
            IShipPositionSource shipPositionSource,
            IBugNestCutsceneShots bugNestCutsceneShots,
            IBugNestFlockTargetIds bugNestFlockTargetIds,
            BugNestConfiguration bugNestConfiguration) {
            _bugSpawnService = bugSpawnService;
            _shipThrottleStop = shipThrottleStop;
            _cutsceneDirector = cutsceneDirector;
            _shipPositionSource = shipPositionSource;
            _bugNestCutsceneShots = bugNestCutsceneShots;
            _bugNestFlockTargetIds = bugNestFlockTargetIds;
            _bugNestConfiguration = bugNestConfiguration;
        }

        public void ServerEnter(in PointOfInterestEntry point) {
            if (!_shipPositionSource.TryGetShipPosition(out Vector3 shipPosition))
                throw new InvalidOperationException($"Point of interest {point.Id} started with no ship: the zone is checked against the ship.");

            _bugSpawnService.ServerSpawnGroupAt(_bugNestConfiguration.BugCount, BehindNest(point.Position, shipPosition), _spawnedIds);
            if (!_bugNestConfiguration.PlayEntryCutscene)
                return;

            _shipThrottleStop.ServerStop();
            _bugNestCutsceneShots.Create(point.Position, shipPosition, _bugNestFlockTargetIds.GetTargetId(point.Id), _shots);
            _cutsceneDirector.ServerPlay(_shots);
        }

        private Vector3 BehindNest(Vector3 nest, Vector3 shipPosition) {
            Vector3 away = nest - shipPosition;
            away.y = 0f;
            return away.sqrMagnitude < DIRECTION_EPSILON ? nest : nest + away.normalized * _bugNestConfiguration.SpawnBehindMeters;
        }
    }
}
