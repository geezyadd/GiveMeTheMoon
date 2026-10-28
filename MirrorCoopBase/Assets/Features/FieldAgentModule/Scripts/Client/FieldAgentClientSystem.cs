using System;
using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Network.Generated;
using Features.GameCoreModule.Contracts;
using Features.ShipModule.Scripts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.FieldAgentModule.Scripts.Client {
    // Every peer with a client (the host too): keeps the received agent batches, shows each agent a fixed delay in the
    // past between two batches through a pooled look of its kind, and puts ship-space agents on this peer's ship, so
    // an agent on the deck stays on the deck as this peer shows it.
    public sealed class FieldAgentClientSystem : IInitializable, IDisposable, ILateTickable, IWorldShiftFollower, IFieldAgentShownAgents {
        private readonly IReadOnlyFieldAgentsModel _fieldAgentsModel;
        private readonly IShipClientBindingModel _shipClientBindingModel;
        private readonly IFieldAgentViewPool _fieldAgentViewPool;
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;
        private readonly List<FieldAgentPose> _poses = new();
        private readonly Dictionary<int, FieldAgentView> _views = new();
        private readonly Dictionary<int, byte> _viewKinds = new();
        private readonly HashSet<int> _shown = new();
        private readonly List<int> _gone = new();
        private FieldAgentFrameBuffer _frameBuffer;

        public FieldAgentClientSystem(
            IReadOnlyFieldAgentsModel fieldAgentsModel,
            IShipClientBindingModel shipClientBindingModel,
            IFieldAgentViewPool fieldAgentViewPool,
            FieldAgentConfiguration fieldAgentConfiguration) {
            _fieldAgentsModel = fieldAgentsModel;
            _shipClientBindingModel = shipClientBindingModel;
            _fieldAgentViewPool = fieldAgentViewPool;
            _fieldAgentConfiguration = fieldAgentConfiguration;
        }

        public int ShownCount => _views.Count;

        public void Initialize() {
            _frameBuffer = new FieldAgentFrameBuffer(_fieldAgentConfiguration.FrameBufferSize);
            _fieldAgentsModel.OnFrameChanged += HandleFrameChanged;
        }

        public void Dispose() =>
            _fieldAgentsModel.OnFrameChanged -= HandleFrameChanged;

        public void LateTick() {
            if (NetworkClient.active == false) {
                _frameBuffer.Clear();
                _poses.Clear();
                ReturnUnshown();
                return;
            }

            _frameBuffer.Sample(NetworkTime.time - _fieldAgentConfiguration.InterpolationDelaySeconds, _poses);
            ShowPoses(_shipClientBindingModel.Ship);
            ReturnUnshown();
        }

        public void FollowWorldShift(Vector3 delta) =>
            _frameBuffer.Shift(delta);

        public bool TryGetShownWorldPosition(int agentId, out Vector3 worldPosition) {
            worldPosition = Vector3.zero;
            if (_views.TryGetValue(agentId, out FieldAgentView view) == false || view == null)
                return false;

            worldPosition = view.transform.position;
            return true;
        }

        public void CollectShownIds(List<int> ids) {
            ids.Clear();
            ids.AddRange(_views.Keys);
        }

        private void ShowPoses(ShipBase ship) {
            _shown.Clear();
            float deltaTime = Time.deltaTime;
            float time = Time.time;
            foreach (FieldAgentPose pose in _poses) {
                if (pose.IsInShipSpace && ship == null)
                    continue;

                Vector3 position = pose.IsInShipSpace ? ship.transform.TransformPoint(pose.Position) : pose.Position;
                Quaternion rotation = pose.IsInShipSpace ? ship.transform.rotation * pose.Rotation : pose.Rotation;
                ReadView(pose).ApplyPose(position, rotation, deltaTime, time);
                _shown.Add(pose.Id);
            }
        }

        private FieldAgentView ReadView(FieldAgentPose pose) {
            if (_views.TryGetValue(pose.Id, out FieldAgentView view) && view != null)
                return view;

            view = _fieldAgentViewPool.Take(pose.KindIndex, pose.Id);
            _views[pose.Id] = view;
            _viewKinds[pose.Id] = pose.KindIndex;
            return view;
        }

        private void ReturnUnshown() {
            _gone.Clear();
            foreach (int id in _views.Keys) {
                if (_shown.Contains(id) == false)
                    _gone.Add(id);
            }

            foreach (int id in _gone) {
                _fieldAgentViewPool.Return(_viewKinds[id], _views[id]);
                _views.Remove(id);
                _viewKinds.Remove(id);
            }

            _shown.Clear();
        }

        private void HandleFrameChanged() =>
            _frameBuffer.Add(_fieldAgentsModel.Frame);
    }
}
