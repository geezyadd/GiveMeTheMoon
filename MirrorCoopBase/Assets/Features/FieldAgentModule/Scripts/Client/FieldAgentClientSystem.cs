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
    // an agent on the deck stays on the deck as this peer shows it. What is shown lives in FieldAgentShownModel.
    public sealed class FieldAgentClientSystem : IInitializable, IDisposable, ILateTickable, IWorldShiftFollower {
        private readonly IReadOnlyFieldAgentsModel _fieldAgentsModel;
        private readonly FieldAgentShownModel _fieldAgentShownModel;
        private readonly IShipClientBindingModel _shipClientBindingModel;
        private readonly IFieldAgentViewPool _fieldAgentViewPool;
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;
        private readonly List<FieldAgentPose> _poses = new();
        private readonly HashSet<int> _shown = new();
        private readonly List<int> _ids = new();

        public FieldAgentClientSystem(
            IReadOnlyFieldAgentsModel fieldAgentsModel,
            FieldAgentShownModel fieldAgentShownModel,
            IShipClientBindingModel shipClientBindingModel,
            IFieldAgentViewPool fieldAgentViewPool,
            FieldAgentConfiguration fieldAgentConfiguration) {
            _fieldAgentsModel = fieldAgentsModel;
            _fieldAgentShownModel = fieldAgentShownModel;
            _shipClientBindingModel = shipClientBindingModel;
            _fieldAgentViewPool = fieldAgentViewPool;
            _fieldAgentConfiguration = fieldAgentConfiguration;
        }

        public void Initialize() =>
            _fieldAgentsModel.OnFrameChanged += HandleFrameChanged;

        public void Dispose() =>
            _fieldAgentsModel.OnFrameChanged -= HandleFrameChanged;

        public void LateTick() {
            _shown.Clear();
            if (NetworkClient.active == false) {
                _fieldAgentShownModel.FrameBuffer.Clear();
                ReturnUnshown();
                return;
            }

            _fieldAgentShownModel.FrameBuffer.Sample(NetworkTime.time - _fieldAgentConfiguration.InterpolationDelaySeconds, _poses);
            ShowPoses(_shipClientBindingModel.Ship);
            ReturnUnshown();
        }

        public void FollowWorldShift(Vector3 delta) =>
            _fieldAgentShownModel.FrameBuffer.Shift(delta);

        private void ShowPoses(ShipBase ship) {
            float deltaTime = Time.deltaTime;
            float time = Time.time;
            foreach (FieldAgentPose pose in _poses) {
                if (pose.IsInShipSpace && ship == null)
                    continue;

                Vector3 position = pose.IsInShipSpace ? ship.transform.TransformPoint(pose.Position) : pose.Position;
                Quaternion rotation = pose.IsInShipSpace ? ship.transform.rotation * pose.Rotation : pose.Rotation;
                TakeOrReuseView(pose).ApplyPose(position, rotation, pose.Mode, deltaTime, time);
                _shown.Add(pose.Id);
            }
        }

        private FieldAgentView TakeOrReuseView(FieldAgentPose pose) {
            if (_fieldAgentShownModel.TryGetView(pose.Id, out FieldAgentView view))
                return view;

            view = _fieldAgentViewPool.Take(pose.KindIndex, pose.Id);
            _fieldAgentShownModel.AddView(pose.Id, pose.KindIndex, view);
            return view;
        }

        private void ReturnUnshown() {
            _fieldAgentShownModel.CollectShownIds(_ids);
            foreach (int id in _ids) {
                if (_shown.Contains(id))
                    continue;

                _fieldAgentShownModel.TryGetView(id, out FieldAgentView view);
                _fieldAgentViewPool.Return(_fieldAgentShownModel.GetViewKind(id), view);
                _fieldAgentShownModel.RemoveView(id);
            }
        }

        private void HandleFrameChanged() =>
            _fieldAgentShownModel.FrameBuffer.Add(_fieldAgentsModel.Frame);
    }
}
