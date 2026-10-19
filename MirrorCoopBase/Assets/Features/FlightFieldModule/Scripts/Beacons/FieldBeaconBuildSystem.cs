using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Scene;
using Features.GameCoreModule.Contracts;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Beacons {
    // Keeps this peer's beacons equal to the live field around the viewpoint: a new field number releases the old
    // beacons and lays out the new ones from the field's routes and cells; then every frame the beacons within the
    // field's build radius are built and the ones beyond its release radius go back to the pool, and both colours
    // blink. The layout waits for the next tick, so the order in which the field's state and lists arrive does not
    // matter; the root follows the field's pose at once, so a world shift moves the beacons with the field.
    public sealed class FieldBeaconBuildSystem : IInitializable, IDisposable, ITickable {
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly FieldBeaconSceneModel _fieldBeaconSceneModel;
        private readonly IFieldBeaconSceneBuilder _fieldBeaconSceneBuilder;
        private readonly IFieldBeaconSource _fieldBeaconSource;
        private readonly IFieldStructureSceneBuilder _fieldStructureSceneBuilder;
        private readonly ILocalViewpointSource _localViewpointSource;
        private readonly IFieldStreamingRule _fieldStreamingRule;
        private readonly List<FieldBeacon> _layout = new();

        private bool _isDirty;

        private bool IsShowingLiveField => _fieldBeaconSceneModel.BuiltNumber != LiveFieldNumbers.NO_FIELD && _fieldBeaconSceneModel.BuiltNumber == _liveFieldModel.Number;

        public FieldBeaconBuildSystem(IReadOnlyLiveFieldModel liveFieldModel, FieldBeaconSceneModel fieldBeaconSceneModel, IFieldBeaconSceneBuilder fieldBeaconSceneBuilder,
                                      IFieldBeaconSource fieldBeaconSource, IFieldStructureSceneBuilder fieldStructureSceneBuilder,
                                      ILocalViewpointSource localViewpointSource, IFieldStreamingRule fieldStreamingRule) {
            _liveFieldModel = liveFieldModel;
            _fieldBeaconSceneModel = fieldBeaconSceneModel;
            _fieldBeaconSceneBuilder = fieldBeaconSceneBuilder;
            _fieldBeaconSource = fieldBeaconSource;
            _fieldStructureSceneBuilder = fieldStructureSceneBuilder;
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

            if (_fieldBeaconSceneModel.BuiltNumber == LiveFieldNumbers.NO_FIELD)
                return;

            StreamBeacons();
            _fieldBeaconSceneBuilder.ApplyBlink(Time.time);
        }

        private void FollowLiveFieldNumber() {
            int liveNumber = _liveFieldModel.IsAvailable ? _liveFieldModel.Number : LiveFieldNumbers.NO_FIELD;
            if (liveNumber == _fieldBeaconSceneModel.BuiltNumber)
                return;

            _fieldBeaconSceneBuilder.ReleaseAll();
            _fieldBeaconSceneModel.Clear();
            if (liveNumber == LiveFieldNumbers.NO_FIELD)
                return;

            if (!_fieldBeaconSceneModel.HasRoot)
                _fieldBeaconSceneModel.SetRoot(_fieldBeaconSceneBuilder.CreateRoot());

            _fieldStructureSceneBuilder.PlaceRoot(_fieldBeaconSceneModel.Root, _liveFieldModel);
            _fieldBeaconSource.Read(_liveFieldModel, _layout);
            _fieldBeaconSceneModel.BeginBuild(liveNumber, _layout);
        }

        // Few beacons per field (tens to a few hundred), all checked every frame without a build budget.
        private void StreamBeacons() {
            Vector3 viewpoint = _fieldBeaconSceneModel.Root.InverseTransformPoint(_localViewpointSource.GetPosition());
            Vector2 viewpointOnPlane = new(viewpoint.x, viewpoint.z);
            IReadOnlyList<FieldBeacon> beacons = _fieldBeaconSceneModel.Beacons;
            for (int i = 0; i < beacons.Count; i++) {
                Vector3 position = beacons[i].Position;
                float distanceSquared = (new Vector2(position.x, position.z) - viewpointOnPlane).sqrMagnitude;
                bool isBuilt = _fieldBeaconSceneModel.IsBeaconBuilt(i);
                if (isBuilt && _fieldStreamingRule.ShouldRelease(distanceSquared)) {
                    _fieldBeaconSceneBuilder.Release(i);
                    _fieldBeaconSceneModel.RemoveBeacon(i);
                }
                else if (!isBuilt && _fieldStreamingRule.ShouldBuild(distanceSquared)) {
                    _fieldBeaconSceneBuilder.Build(_fieldBeaconSceneModel.Root, i, beacons[i]);
                    _fieldBeaconSceneModel.AddBeacon(i);
                }
            }
        }

        private void OnLiveFieldChanged() {
            _isDirty = true;
            if (IsShowingLiveField)
                _fieldStructureSceneBuilder.PlaceRoot(_fieldBeaconSceneModel.Root, _liveFieldModel);
        }
    }
}
