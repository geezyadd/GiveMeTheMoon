using System.Collections.Generic;
using Features.BugModule.Scripts.Behaviours;
using Features.BugModule.Scripts.Brain;
using Features.BugModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Server;
using Features.FieldAgentModule.Scripts.Spawn;
using Features.ShipModule.Scripts;
using UnityEngine;

namespace Features.BugModule.Scripts.Spawn {
    // A bug group comes out of the field agents' hidden spawn (behind a rock from every player, else past the haze),
    // and each bug gets its own point on the ring round the ship, spread over an arc centred on the side the group
    // comes from, so the group parts on the way and comes at the ship from different sides.
    public sealed class BugSpawnService : IBugSpawnService {
        private const float FULL_TURN_DEGREES = 360f;

        private readonly IFieldAgentGroupSpawnService _fieldAgentGroupSpawnService;
        private readonly IFieldAgentControl _fieldAgentControl;
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IBugKindService _bugKindService;
        private readonly IBugBehaviourFactory _bugBehaviourFactory;
        private readonly BugServerModel _bugServerModel;
        private readonly BugConfiguration _bugConfiguration;
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;

        public BugSpawnService(
            IFieldAgentGroupSpawnService fieldAgentGroupSpawnService,
            IFieldAgentControl fieldAgentControl,
            IShipRunBindingModel shipRunBindingModel,
            IBugKindService bugKindService,
            IBugBehaviourFactory bugBehaviourFactory,
            BugServerModel bugServerModel,
            BugConfiguration bugConfiguration,
            FieldAgentConfiguration fieldAgentConfiguration) {
            _fieldAgentGroupSpawnService = fieldAgentGroupSpawnService;
            _fieldAgentControl = fieldAgentControl;
            _shipRunBindingModel = shipRunBindingModel;
            _bugKindService = bugKindService;
            _bugBehaviourFactory = bugBehaviourFactory;
            _bugServerModel = bugServerModel;
            _bugConfiguration = bugConfiguration;
            _fieldAgentConfiguration = fieldAgentConfiguration;
        }

        public FieldAgentSpawnCover ServerSpawnGroup(int count, List<int> spawnedIds) {
            spawnedIds.Clear();
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null || count <= 0 || _bugKindService.TryGetKindIndex(out byte kindIndex) == false)
                return FieldAgentSpawnCover.None;

            FieldAgentProfile profile = _fieldAgentConfiguration.Profiles[kindIndex];
            FieldAgentSpawnCover cover = _fieldAgentGroupSpawnService.SpawnHiddenGroup(profile, count, ship.transform.position, _bugServerModel.TakeSpawnSeed(), CreateFirstApproach, spawnedIds);
            AddGroup(ship.transform, spawnedIds);
            return cover;
        }

        public void ServerSpawnGroupAt(int count, Vector3 point, List<int> spawnedIds) {
            spawnedIds.Clear();
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null || count <= 0 || _bugKindService.TryGetKindIndex(out byte kindIndex) == false)
                return;

            FieldAgentProfile profile = _fieldAgentConfiguration.Profiles[kindIndex];
            _fieldAgentGroupSpawnService.SpawnGroupAt(profile, count, point, _bugServerModel.TakeSpawnSeed(), CreateFirstApproach, spawnedIds);
            AddGroup(ship.transform, spawnedIds);
        }

        private void AddGroup(Transform ship, List<int> spawnedIds) {
            if (spawnedIds.Count == 0)
                return;

            float centerDegrees = CalculateBearingDegrees(ship, spawnedIds[0]);
            for (int i = 0; i < spawnedIds.Count; i++)
                AddBug(spawnedIds[i], centerDegrees + CalculateSpreadDegrees(i, spawnedIds.Count));
        }

        private void AddBug(int id, float approachAngleDegrees) {
            _bugServerModel.Add(new BugAgent(id, Mathf.Repeat(approachAngleDegrees, FULL_TURN_DEGREES), _bugConfiguration.MaxHealth));
            _fieldAgentControl.SetBehaviour(id, _bugBehaviourFactory.CreateApproach(approachAngleDegrees));
        }

        // From one end of the arc to the other; a lone bug comes straight in.
        private float CalculateSpreadDegrees(int index, int count) =>
            count <= 1 ? 0f : (index / (float)(count - 1) - 0.5f) * _bugConfiguration.ApproachSpreadDegrees;

        // The ship-local bearing of the spawn point, round the ship's up, from its nose.
        private float CalculateBearingDegrees(Transform ship, int firstId) {
            _fieldAgentControl.TryGetWorldPosition(firstId, out Vector3 spawnPoint);
            Vector3 local = ship.InverseTransformDirection(spawnPoint - ship.position);
            return Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        }

        // Replaced per bug right after the group appears, once each bug's side is known.
        private IFieldAgentBehaviour CreateFirstApproach() =>
            _bugBehaviourFactory.CreateApproach(0f);
    }
}
