using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Scene;
using Features.GameCoreModule.Contracts;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Keeps this peer's target balloons equal to the live field around the viewpoint, like the field's structures and
    // by the same build and release radii (IFieldStreamingRule): a
    // new field number releases the old balloons; then every frame the balloons within the build radius are built
    // and the built ones beyond the release radius go back to the pool. A popped balloon is never built, so a balloon
    // that streams in later (or a late joiner's snapshot) stays gone; the pop view hides one that pops while it
    // stands. The root follows the live field's origin at once, so a world shift moves the balloons with the field.
    public sealed class FieldTargetBuildSystem : IInitializable, IDisposable, ITickable {
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly FieldTargetSceneModel _fieldTargetSceneModel;
        private readonly IFieldTargetSceneBuilder _fieldTargetSceneBuilder;
        private readonly IFieldPoppedTargets _fieldPoppedTargets;
        private readonly ILocalViewpointSource _localViewpointSource;
        private readonly IFieldStreamingRule _fieldStreamingRule;

        private bool _isDirty;

        private bool IsShowingLiveField => _fieldTargetSceneModel.BuiltNumber != LiveFieldNumbers.NO_FIELD && _fieldTargetSceneModel.BuiltNumber == _liveFieldModel.Number;

        public FieldTargetBuildSystem(IReadOnlyLiveFieldModel liveFieldModel, FieldTargetSceneModel fieldTargetSceneModel, IFieldTargetSceneBuilder fieldTargetSceneBuilder,
                                      IFieldPoppedTargets fieldPoppedTargets, ILocalViewpointSource localViewpointSource, IFieldStreamingRule fieldStreamingRule) {
            _liveFieldModel = liveFieldModel;
            _fieldTargetSceneModel = fieldTargetSceneModel;
            _fieldTargetSceneBuilder = fieldTargetSceneBuilder;
            _fieldPoppedTargets = fieldPoppedTargets;
            _localViewpointSource = localViewpointSource;
            _fieldStreamingRule = fieldStreamingRule;
        }

        public void Initialize() =>
            _liveFieldModel.OnChanged += OnLiveFieldChanged;

        public void Dispose() =>
            _liveFieldModel.OnChanged -= OnLiveFieldChanged;

        public void Tick() {
            if (_isDirty) {
                _isDirty = false;
                FollowLiveFieldNumber();
            }

            if (IsShowingLiveField)
                StreamTargets();
        }

        private void FollowLiveFieldNumber() {
            int liveNumber = _liveFieldModel.IsAvailable ? _liveFieldModel.Number : LiveFieldNumbers.NO_FIELD;
            if (liveNumber == _fieldTargetSceneModel.BuiltNumber)
                return;

            ReleaseAll();
            _fieldTargetSceneModel.Clear();
            if (liveNumber == LiveFieldNumbers.NO_FIELD)
                return;

            if (!_fieldTargetSceneModel.HasRoot)
                _fieldTargetSceneModel.SetRoot(_fieldTargetSceneBuilder.CreateRoot());

            _fieldTargetSceneBuilder.PlaceRoot(_fieldTargetSceneModel.Root, _liveFieldModel);
            _fieldTargetSceneModel.BeginBuild(liveNumber);
        }

        private void StreamTargets() {
            IReadOnlyList<FieldTargetEntry> targets = _liveFieldModel.Targets;
            _fieldTargetSceneModel.TrackTargets(targets.Count);
            Vector3 viewpoint = _fieldTargetSceneModel.Root.InverseTransformPoint(_localViewpointSource.GetPosition());
            for (int i = 0; i < targets.Count; i++) {
                bool isBuilt = _fieldTargetSceneModel.TryGetView(i, out FieldTargetView view);
                float distanceSquared = PlaneDistanceSquared(targets[i].FieldPosition, viewpoint);
                if (isBuilt && _fieldStreamingRule.ShouldRelease(distanceSquared))
                    Release(i, view);
                else if (!isBuilt && _fieldStreamingRule.ShouldBuild(distanceSquared) && !_fieldPoppedTargets.IsPopped(new FieldTargetId(i)))
                    _fieldTargetSceneModel.SetView(i, _fieldTargetSceneBuilder.Build(_fieldTargetSceneModel.Root, targets[i], i));
            }
        }

        private void Release(int index, FieldTargetView view) {
            _fieldTargetSceneBuilder.Release(view);
            _fieldTargetSceneModel.SetView(index, null);
        }

        private void ReleaseAll() {
            for (int i = 0; i < _fieldTargetSceneModel.Views.Count; i++) {
                if (_fieldTargetSceneModel.TryGetView(i, out FieldTargetView view))
                    Release(i, view);
            }
        }

        // On the flight plane: field space x across, z along.
        private static float PlaneDistanceSquared(Vector3 target, Vector3 viewpoint) =>
            new Vector2(target.x - viewpoint.x, target.z - viewpoint.z).sqrMagnitude;

        private void OnLiveFieldChanged() {
            _isDirty = true;
            if (IsShowingLiveField)
                _fieldTargetSceneBuilder.PlaceRoot(_fieldTargetSceneModel.Root, _liveFieldModel);
        }
    }
}
