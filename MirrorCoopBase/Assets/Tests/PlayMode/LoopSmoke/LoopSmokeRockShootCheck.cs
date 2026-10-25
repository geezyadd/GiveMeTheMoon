#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Structures;
using Features.NetworkModelModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Turret;
using Features.WeaponModule.Contracts;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // Weapon damage on the field: the seated host aims the minigun at a small rock (the station's test hook stands in
    // for the mouse) and holds the trigger until the server breaks the rock. The rock is in the synced broken list,
    // hidden on the host, and RockDestroyed fired once for it as a Weapon break by the host with the minigun.
    // The check brings its own target instead of hoping the field has one in sight: it puts the ship beside the nearest
    // small rocks of the field in turn (the debug move of the balloon shot) until one is in reach and clear sight of
    // the turret, and puts the ship back afterwards.
    public sealed class LoopSmokeRockShootCheck {
        private const string MINIGUN_ID = "minigun";
        private const int FIELD_ROCK_LAYER_MASK = 1 << 8;
        private const float MIN_DISTANCE = 25f;
        private const float MAX_DISTANCE = 250f;
        // Within the turret's pitch limits (-10 degrees) with a margin.
        private const float MIN_ELEVATION_DEGREES = -6f;
        private const float BREAK_TIMEOUT_SECONDS = 5f;
        private const float CANDIDATE_SEARCH_RADIUS = 3000f;
        private const int CANDIDATES_TRIED = 12;
        private const float SHOT_DISTANCE = 45f;
        // The turret on the deck looks a little up at the rock: the ship stands this much lower.
        private const float SHOT_HEIGHT_BELOW = 4f;
        // The local field builds the rocks around the moved ship over a few frames.
        private const float SIGHT_WAIT_SECONDS = 0.5f;

        private readonly LoopSmokeSession _session;
        private readonly LoopSmokeFlight _flight;
        private readonly IFieldRockQuery _fieldRockQuery;
        private readonly IFieldRockEvents _fieldRockEvents;
        private readonly IFieldRockTargetIds _fieldRockTargetIds;
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly IFieldStructureSceneBuilder _fieldStructureSceneBuilder;
        private readonly IFieldDebrisSceneBuilder _fieldDebrisSceneBuilder;
        private readonly IPlayerIdentityService _playerIdentityService;
        private readonly List<FieldRockDestroyed> _destroyed = new List<FieldRockDestroyed>();
        private readonly List<FieldRockDescription> _rocks = new List<FieldRockDescription>();
        private FieldRockDescription _target;
        private bool _hasTarget;

        public LoopSmokeRockShootCheck(LoopSmokeContext context) {
            _session = context.Session;
            _flight = context.Flight;
            _fieldRockQuery = SessionQueries.Resolve<IFieldRockQuery>();
            _fieldRockEvents = SessionQueries.Resolve<IFieldRockEvents>();
            _fieldRockTargetIds = SessionQueries.Resolve<IFieldRockTargetIds>();
            _liveFieldModel = SessionQueries.Resolve<IReadOnlyLiveFieldModel>();
            _fieldStructureSceneBuilder = SessionQueries.Resolve<IFieldStructureSceneBuilder>();
            _fieldDebrisSceneBuilder = SessionQueries.Resolve<IFieldDebrisSceneBuilder>();
            _playerIdentityService = SessionQueries.Resolve<IPlayerIdentityService>();
        }

        // After the turret check sat the host down; before the balloon shot.
        [LoopSmokeStep(LoopSmokePhase.InCruise, 2, 2, 20)]
        public IEnumerator ShootNearestSmallRockCoroutine() {
            ShipBase ship = _session.Ship;
            ShipTurretStation station = LoopSmokeTurretCheck.FindSeatedStation(ship);
            Vector3 start = ship.transform.position;
            _hasTarget = false;
            yield return PutShipBesideSmallRockCoroutine(ship, station);
            Assert.IsTrue(_hasTarget, $"Rock shot: none of the {CANDIDATES_TRIED} nearest small breakable rocks came into sight of the turret with the ship beside it.");
            yield return ShootUntilBrokenCoroutine(station, _target);
            yield return PutShipBackCoroutine(ship, start);
        }

        private IEnumerator ShootUntilBrokenCoroutine(ShipTurretStation station, FieldRockDescription rock) {
            _destroyed.Clear();
            _fieldRockEvents.RockDestroyed += _destroyed.Add;
            station.DebugHoldTrigger(true);
            yield return _session.WaitForCoroutine(() => AimAndCheckBroken(station, rock), BREAK_TIMEOUT_SECONDS, $"Rock shot: the minigun broke rock {rock.Id}");
            station.DebugHoldTrigger(false);
            yield return null;
            _fieldRockEvents.RockDestroyed -= _destroyed.Add;

            List<FieldRockDestroyed> ofRock = _destroyed.Where(destroyed => destroyed.Id.Equals(rock.Id)).ToList();
            Assert.AreEqual(1, ofRock.Count, $"RockDestroyed must fire once for rock {rock.Id}.");
            Assert.AreEqual(FieldRockDestroyCause.Weapon, ofRock[0].Cause);
            Assert.AreEqual(_playerIdentityService.GetKey(NetworkServer.localConnection), ofRock[0].Instigator, "The rock was not broken by the host.");
            Assert.AreEqual(MINIGUN_ID, ofRock[0].WeaponId);
            Assert.IsTrue(TryGetBuiltRock(rock.Id, out FieldRockVisual built) && !built.gameObject.activeSelf, $"Shot rock {rock.Id} is still shown.");
            Debug.Log($"[LoopSmoke] the minigun broke rock {rock.Id}: {rock.SourceId}, {rock.Diameter:0.0} m, {_destroyed.Count} rocks broken while firing");
        }

        // Sets _target and _hasTarget once a rock is in sight.
        private IEnumerator PutShipBesideSmallRockCoroutine(ShipBase ship, ShipTurretStation station) {
            foreach (FieldRockDescription candidate in FindSmallRockCandidates(ship.transform.position)) {
                PutShipBeside(ship, candidate);
                for (float waited = 0f; waited < SIGHT_WAIT_SECONDS; waited += Time.deltaTime) {
                    yield return null;
                    if (IsInSight(station.transform, candidate) == false)
                        continue;

                    _target = candidate;
                    _hasTarget = true;
                    yield break;
                }
            }
        }

        private IEnumerator PutShipBackCoroutine(ShipBase ship, Vector3 start) {
            ship.FlightControlDebug.DebugMove(start - ship.transform.position);
            // The flight applies the move in its next step; the pilot faces the destination from there.
            yield return null;
            _flight.FaceDestination();
        }

        private List<FieldRockDescription> FindSmallRockCandidates(Vector3 from) {
            _fieldRockQuery.FindInSphere(from, CANDIDATE_SEARCH_RADIUS, _rocks);
            return _rocks.Where(IsUnbrokenSmallRock)
                .OrderBy(rock => (rock.Center - from).sqrMagnitude)
                .Take(CANDIDATES_TRIED)
                .ToList();
        }

        private bool IsUnbrokenSmallRock(FieldRockDescription rock) =>
            rock.IsBreakable && rock.Size == FieldRockSize.Small && _liveFieldModel.BrokenRocks.Contains(rock.Id) == false;

        // Beside the rock and heading across the line to it, a little lower, so the turret looks up at it.
        private static void PutShipBeside(ShipBase ship, FieldRockDescription rock) {
            Vector3 toRock = rock.Center - ship.transform.position;
            toRock.y = 0f;
            Vector3 direction = toRock.sqrMagnitude > 0f ? toRock.normalized : Vector3.forward;
            Vector3 position = rock.Center - direction * SHOT_DISTANCE + Vector3.down * SHOT_HEIGHT_BELOW;
            ship.FlightControlDebug.DebugMove(position - ship.transform.position);
            ship.FlightControlDebug.DebugFace(Vector3.Cross(Vector3.up, direction));
        }

        private bool AimAndCheckBroken(ShipTurretStation station, FieldRockDescription rock) {
            station.DebugAimAt(rock.Center);
            return _liveFieldModel.BrokenRocks.Contains(rock.Id);
        }

        // Above the turret's lowest pitch, and its own trigger is the first thing a ray meets.
        private bool IsInSight(Transform turret, FieldRockDescription rock) =>
            IsSmallTargetInReach(turret, rock) && IsFirstStruck(turret.position, rock);

        private bool IsSmallTargetInReach(Transform turret, FieldRockDescription rock) {
            Vector3 toRock = rock.Center - turret.position;
            float elevation = 90f - Vector3.Angle(turret.up, toRock);
            bool isBuiltAndShown = TryGetBuiltRock(rock.Id, out FieldRockVisual built) && built.gameObject.activeInHierarchy;
            return rock.IsBreakable && rock.Size == FieldRockSize.Small && isBuiltAndShown && toRock.magnitude >= MIN_DISTANCE && toRock.magnitude <= MAX_DISTANCE
                   && elevation >= MIN_ELEVATION_DEGREES;
        }

        private bool IsFirstStruck(Vector3 origin, FieldRockDescription rock) {
            Vector3 toRock = rock.Center - origin;
            RaycastHit[] hits = Physics.RaycastAll(origin, toRock.normalized, toRock.magnitude, FIELD_ROCK_LAYER_MASK, QueryTriggerInteraction.Collide);
            foreach (RaycastHit hit in hits.OrderBy(hit => hit.distance)) {
                bool hasTarget = hit.collider.TryGetComponent(out IWeaponTargetCollider target);
                if (hit.collider.isTrigger && !hasTarget)
                    continue;

                return hasTarget && target.TargetId == _fieldRockTargetIds.GetTargetId(rock.Id);
            }

            return false;
        }

        private bool TryGetBuiltRock(FieldRockId id, out FieldRockVisual rock) =>
            id.Source == FieldRockSource.Cluster
                ? _fieldStructureSceneBuilder.TryGetBuiltRock(id.PlacementIndex, id.RockIndex, out rock)
                : _fieldDebrisSceneBuilder.TryGetBuiltRock(id.DebrisCell, id.RockIndex, out rock);
    }
}
#endif
