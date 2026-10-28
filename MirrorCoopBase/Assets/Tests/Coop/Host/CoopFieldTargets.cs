#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Structures;
using Features.FlightFieldModule.Scripts.Targets;
using Features.WeaponModule.Contracts;
using Tests.Shared;
using UnityEngine;

namespace Tests.Coop.Host {
    // The host's view of the field's targets for the client's shots: the nearest small breakable rocks and balloons, and
    // whether one is built, shown and the first thing a shot from a point meets (the loop test's rock and balloon shot
    // checks pick their targets the same way).
    public sealed class CoopFieldTargets {
        private const int FIELD_ROCK_LAYER_MASK = 1 << 8;
        private const float SEARCH_RADIUS = 3000f;
        private const int CANDIDATES_TRIED = 12;
        private const float MIN_DISTANCE = 20f;
        private const float MAX_DISTANCE = 140f;
        // Within the turret's pitch limits (-10 degrees) with a margin.
        private const float MIN_ELEVATION_DEGREES = -6f;

        private readonly IFieldRockQuery _rockQuery = SessionQueries.Resolve<IFieldRockQuery>();
        private readonly IFieldTargetQuery _targetQuery = SessionQueries.Resolve<IFieldTargetQuery>();
        private readonly IFieldRockTargetIds _rockTargetIds = SessionQueries.Resolve<IFieldRockTargetIds>();
        private readonly IFieldTargetWeaponIds _targetWeaponIds = SessionQueries.Resolve<IFieldTargetWeaponIds>();
        private readonly IReadOnlyLiveFieldModel _field = SessionQueries.Resolve<IReadOnlyLiveFieldModel>();
        private readonly IFieldStructureSceneBuilder _structures = SessionQueries.Resolve<IFieldStructureSceneBuilder>();
        private readonly IFieldDebrisSceneBuilder _debris = SessionQueries.Resolve<IFieldDebrisSceneBuilder>();
        private readonly IReadOnlyFieldTargetSceneModel _balloons = SessionQueries.Resolve<IReadOnlyFieldTargetSceneModel>();
        private readonly List<FieldRockDescription> _rocks = new List<FieldRockDescription>();
        private readonly List<FieldTargetDescription> _targets = new List<FieldTargetDescription>();

        public List<FieldRockDescription> FindSmallRocks(Vector3 from) {
            _rockQuery.FindInSphere(from, SEARCH_RADIUS, _rocks);
            return _rocks.Where(rock => rock.IsBreakable && rock.Size == FieldRockSize.Small && _field.BrokenRocks.Contains(rock.Id) == false)
                .OrderBy(rock => (rock.Center - from).sqrMagnitude)
                .Take(CANDIDATES_TRIED)
                .ToList();
        }

        public List<FieldTargetDescription> FindBalloons(Vector3 from) {
            _targetQuery.FindInSphere(from, SEARCH_RADIUS, _targets);
            return _targets.OrderBy(target => (target.Center - from).sqrMagnitude).Take(CANDIDATES_TRIED).ToList();
        }

        public bool IsRockInSight(Transform shooter, FieldRockDescription rock) {
            Vector3 toRock = rock.Center - shooter.position;
            float elevation = 90f - Vector3.Angle(shooter.up, toRock);
            bool isInReach = toRock.magnitude >= MIN_DISTANCE && toRock.magnitude <= MAX_DISTANCE && elevation >= MIN_ELEVATION_DEGREES;
            return isInReach && IsRockShown(rock.Id) && IsFirstStruck(shooter.position, rock.Center, _rockTargetIds.GetTargetId(rock.Id));
        }

        public bool IsBalloonInSight(Transform shooter, FieldTargetDescription balloon) =>
            IsBalloonShown(balloon.Id) && IsFirstStruck(shooter.position, balloon.Center, _targetWeaponIds.GetTargetId(balloon.Id));

        public bool IsRockShown(FieldRockId id) {
            FieldRockVisual rock;
            bool isBuilt;
            if (id.Source == FieldRockSource.Cluster)
                isBuilt = _structures.TryGetBuiltRock(id.PlacementIndex, id.RockIndex, out rock);
            else
                isBuilt = _debris.TryGetBuiltRock(id.DebrisCell, id.RockIndex, out rock);

            return isBuilt && rock.gameObject.activeInHierarchy;
        }

        public bool IsBalloonShown(FieldTargetId id) =>
            _balloons.TryGetView(id.Index, out FieldTargetView _);

        private static bool IsFirstStruck(Vector3 origin, Vector3 center, ulong targetId) {
            Vector3 toTarget = center - origin;
            RaycastHit[] hits = Physics.RaycastAll(origin, toTarget.normalized, toTarget.magnitude, FIELD_ROCK_LAYER_MASK, QueryTriggerInteraction.Collide);
            foreach (RaycastHit hit in hits.OrderBy(hit => hit.distance)) {
                bool hasTarget = hit.collider.TryGetComponent(out IWeaponTargetCollider collider);
                if (hit.collider.isTrigger && hasTarget == false)
                    continue;

                return hasTarget && collider.TargetId == targetId;
            }

            return false;
        }
    }
}
#endif
