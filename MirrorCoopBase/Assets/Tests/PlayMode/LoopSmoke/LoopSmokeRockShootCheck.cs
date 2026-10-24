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
using Features.ShipModule.Scripts.Turret;
using Features.WeaponModule.Contracts;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // Weapon damage on the field: the seated host aims the minigun at the nearest small rock it can see (the station's
    // test hook stands in for the mouse) and holds the trigger until the server breaks the rock. The rock is in the
    // synced broken list, hidden on the host, and RockDestroyed fired once for it as a Weapon break by the host with
    // the minigun.
    public sealed class LoopSmokeRockShootCheck {
        private const string MINIGUN_ID = "minigun";
        private const int FIELD_ROCK_LAYER_MASK = 1 << 8;
        private const float SEARCH_RADIUS = 260f;
        private const float MIN_DISTANCE = 25f;
        private const float MAX_DISTANCE = 250f;
        // Within the turret's pitch limits (-10 degrees) with a margin.
        private const float MIN_ELEVATION_DEGREES = -6f;
        private const float BREAK_TIMEOUT_SECONDS = 5f;

        private readonly LoopSmokeSession _session;
        private readonly IFieldRockQuery _fieldRockQuery;
        private readonly IFieldRockEvents _fieldRockEvents;
        private readonly IFieldRockTargetIds _fieldRockTargetIds;
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly IFieldStructureSceneBuilder _fieldStructureSceneBuilder;
        private readonly IFieldDebrisSceneBuilder _fieldDebrisSceneBuilder;
        private readonly IPlayerIdentityService _playerIdentityService;
        private readonly List<FieldRockDestroyed> _destroyed = new List<FieldRockDestroyed>();
        private readonly List<FieldRockDescription> _rocks = new List<FieldRockDescription>();

        public LoopSmokeRockShootCheck(LoopSmokeSession session) {
            _session = session;
            _fieldRockQuery = SessionQueries.Resolve<IFieldRockQuery>();
            _fieldRockEvents = SessionQueries.Resolve<IFieldRockEvents>();
            _fieldRockTargetIds = SessionQueries.Resolve<IFieldRockTargetIds>();
            _liveFieldModel = SessionQueries.Resolve<IReadOnlyLiveFieldModel>();
            _fieldStructureSceneBuilder = SessionQueries.Resolve<IFieldStructureSceneBuilder>();
            _fieldDebrisSceneBuilder = SessionQueries.Resolve<IFieldDebrisSceneBuilder>();
            _playerIdentityService = SessionQueries.Resolve<IPlayerIdentityService>();
        }

        public IEnumerator ShootNearestSmallRockCoroutine(ShipTurretStation station) {
            Assert.IsTrue(TryFindVisibleSmallRock(station.transform, out FieldRockDescription rock), "Rock shot: no small breakable rock in sight of the turret in cruise.");
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

        private bool AimAndCheckBroken(ShipTurretStation station, FieldRockDescription rock) {
            station.DebugAimAt(rock.Center);
            return _liveFieldModel.BrokenRocks.Contains(rock.Id);
        }

        // The nearest small rock above the turret's lowest pitch whose own trigger is the first thing a ray meets.
        private bool TryFindVisibleSmallRock(Transform turret, out FieldRockDescription found) {
            found = default;
            _fieldRockQuery.FindInSphere(turret.position, SEARCH_RADIUS, _rocks);
            foreach (FieldRockDescription rock in _rocks.OrderBy(rock => (rock.Center - turret.position).sqrMagnitude)) {
                if (!IsSmallTargetInReach(turret, rock) || !IsFirstStruck(turret.position, rock))
                    continue;

                found = rock;
                return true;
            }

            return false;
        }

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
