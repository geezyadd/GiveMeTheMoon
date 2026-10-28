using System;
using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Deck;
using Features.ShipModule.Scripts;
using Mirror;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Server {
    // Server: writes the agent model for the callers of IFieldAgentControl. Only the server moves agents.
    public sealed class FieldAgentControlService : IFieldAgentControl {
        private const string SERVER_ONLY = "Field agents are created and steered on the server only.";
        private const float NOISE_PHASE_RANGE = 1000f;

        private readonly FieldAgentServerModel _fieldAgentServerModel;
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;
        private readonly IFieldAgentTransitionService _fieldAgentTransitionService;
        private readonly IShipRunBindingModel _shipRunBindingModel;

        public int Count => _fieldAgentServerModel.Count;

        public FieldAgentControlService(
            FieldAgentServerModel fieldAgentServerModel,
            FieldAgentConfiguration fieldAgentConfiguration,
            IFieldAgentTransitionService fieldAgentTransitionService,
            IShipRunBindingModel shipRunBindingModel) {
            _fieldAgentServerModel = fieldAgentServerModel;
            _fieldAgentConfiguration = fieldAgentConfiguration;
            _fieldAgentTransitionService = fieldAgentTransitionService;
            _shipRunBindingModel = shipRunBindingModel;
        }

        public int Spawn(FieldAgentProfile profile, Vector3 worldPosition, IFieldAgentBehaviour behaviour) {
            AssertServer();
            FieldAgentState state = new() {
                KindIndex = FindKindIndex(profile),
                Mode = FieldAgentMode.Flight,
                Position = worldPosition,
                Rotation = Quaternion.identity,
                NoisePhase = UnityEngine.Random.value * NOISE_PHASE_RANGE,
            };
            return _fieldAgentServerModel.Add(state, behaviour);
        }

        public bool Despawn(int id) {
            AssertServer();
            if (_fieldAgentServerModel.TryFindIndex(id, out int index) == false)
                return false;

            _fieldAgentServerModel.RemoveAt(index);
            return true;
        }

        public void DespawnAll() {
            AssertServer();
            _fieldAgentServerModel.Clear();
        }

        public bool SetBehaviour(int id, IFieldAgentBehaviour behaviour) {
            AssertServer();
            if (_fieldAgentServerModel.TryFindIndex(id, out int index) == false)
                return false;

            _fieldAgentServerModel.SetBehaviour(index, behaviour);
            return true;
        }

        public bool TakeOff(int id) {
            AssertServer();
            if (_fieldAgentServerModel.TryFindIndex(id, out int index) == false)
                return false;

            ref FieldAgentState state = ref _fieldAgentServerModel.GetState(index);
            if (state.Mode != FieldAgentMode.Deck)
                return false;

            _fieldAgentTransitionService.BeginTakeOff(ref state, _fieldAgentConfiguration.Profiles[state.KindIndex]);
            _fieldAgentServerModel.GetDeckPath(index).Clear();
            return true;
        }

        public bool KnockOff(int id, Vector3 worldVelocity) {
            AssertServer();
            if (_fieldAgentServerModel.TryFindIndex(id, out int index) == false || _shipRunBindingModel.Ship == null)
                return false;

            Transform ship = _shipRunBindingModel.Ship.transform;
            FieldAgentShipFrame frame = new(true, ship.position, ship.rotation, _fieldAgentServerModel.ShipVelocity);
            _fieldAgentTransitionService.KnockOff(ref _fieldAgentServerModel.GetState(index), frame, worldVelocity);
            _fieldAgentServerModel.GetDeckPath(index).Clear();
            return true;
        }

        public bool TryGetAgent(int id, out FieldAgentState state) {
            state = default;
            if (_fieldAgentServerModel.TryFindIndex(id, out int index) == false)
                return false;

            state = _fieldAgentServerModel.GetState(index);
            return true;
        }

        public bool TryGetWorldPosition(int id, out Vector3 worldPosition) {
            worldPosition = Vector3.zero;
            if (TryGetAgent(id, out FieldAgentState state) == false)
                return false;

            if (state.IsInShipSpace == false) {
                worldPosition = state.Position;
                return true;
            }

            if (_shipRunBindingModel.Ship == null)
                return false;

            worldPosition = _shipRunBindingModel.Ship.transform.TransformPoint(state.Position);
            return true;
        }

        private byte FindKindIndex(FieldAgentProfile profile) {
            IReadOnlyList<FieldAgentProfile> profiles = _fieldAgentConfiguration.Profiles;
            for (int i = 0; i < profiles.Count; i++) {
                if (profiles[i] == profile)
                    return (byte)i;
            }

            throw new ArgumentException($"Field agent kind {profile.Id} is not in {nameof(FieldAgentConfiguration.Profiles)}.", nameof(profile));
        }

        private static void AssertServer() {
            if (NetworkServer.active == false)
                throw new InvalidOperationException(SERVER_ONLY);
        }
    }
}
