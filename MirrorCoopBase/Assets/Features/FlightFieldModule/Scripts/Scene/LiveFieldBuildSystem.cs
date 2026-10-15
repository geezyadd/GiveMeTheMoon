using System;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Scene {
    // Keeps this peer's local structures equal to the live field model: a new field number releases the old
    // structures to the pool and builds the new ones a few per frame (the takeoff hides it), no field releases them.
    // A late joiner takes the same path from the model snapshot. The rebuild waits for the next tick, so the order in
    // which the field's state and lists arrive does not matter; the root pose follows the model at once, so a world
    // shift moves the structures in the frame the ship moves.
    public sealed class LiveFieldBuildSystem : IInitializable, IDisposable, ITickable {
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly FlightFieldSceneModel _flightFieldSceneModel;
        private readonly IFieldStructureSceneBuilder _fieldStructureSceneBuilder;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;

        private bool _isDirty;
        private int _nextPlacementIndex;

        private bool IsShowingLiveField => _flightFieldSceneModel.BuiltNumber != LiveFieldNumbers.NO_FIELD && _flightFieldSceneModel.BuiltNumber == _liveFieldModel.Number;

        public LiveFieldBuildSystem(IReadOnlyLiveFieldModel liveFieldModel, FlightFieldSceneModel flightFieldSceneModel, IFieldStructureSceneBuilder fieldStructureSceneBuilder,
                                    FlightFieldConfiguration flightFieldConfiguration) {
            _liveFieldModel = liveFieldModel;
            _flightFieldSceneModel = flightFieldSceneModel;
            _fieldStructureSceneBuilder = fieldStructureSceneBuilder;
            _flightFieldConfiguration = flightFieldConfiguration;
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

            BuildNextStructures();
        }

        private void FollowLiveFieldNumber() {
            int liveNumber = _liveFieldModel.IsAvailable ? _liveFieldModel.Number : LiveFieldNumbers.NO_FIELD;
            if (liveNumber == _flightFieldSceneModel.BuiltNumber)
                return;

            _fieldStructureSceneBuilder.ReleaseAll();
            _flightFieldSceneModel.Clear();
            if (liveNumber == LiveFieldNumbers.NO_FIELD)
                return;

            if (!_flightFieldSceneModel.HasRoot)
                _flightFieldSceneModel.SetRoot(_fieldStructureSceneBuilder.CreateRoot());

            _fieldStructureSceneBuilder.PlaceRoot(_flightFieldSceneModel.Root, _liveFieldModel);
            _nextPlacementIndex = 0;
            _flightFieldSceneModel.BeginBuild(liveNumber);
        }

        private void BuildNextStructures() {
            if (_flightFieldSceneModel.BuiltNumber == LiveFieldNumbers.NO_FIELD || _flightFieldSceneModel.IsBuilt)
                return;

            int placementCount = _liveFieldModel.Placements.Count;
            int end = Mathf.Min(_nextPlacementIndex + _flightFieldConfiguration.StructuresBuiltPerFrame, placementCount);
            for (; _nextPlacementIndex < end; _nextPlacementIndex++) {
                if (_fieldStructureSceneBuilder.TryBuild(_flightFieldSceneModel.Root, _liveFieldModel, _nextPlacementIndex))
                    _flightFieldSceneModel.AddBuiltStructure();
            }

            if (_nextPlacementIndex >= placementCount)
                _flightFieldSceneModel.CompleteBuild();
        }

        private void OnLiveFieldChanged() {
            _isDirty = true;
            if (IsShowingLiveField)
                _fieldStructureSceneBuilder.PlaceRoot(_flightFieldSceneModel.Root, _liveFieldModel);
        }
    }
}
