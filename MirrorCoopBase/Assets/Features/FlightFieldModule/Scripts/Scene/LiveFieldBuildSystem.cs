using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Scene {
    // Keeps this peer's local structures equal to the live field model around the viewpoint: a new field number
    // releases the old structures to the pool; then every frame the placements within the build radius are built, a
    // few per frame, and the built ones beyond the release radius go back to the pool, so a long field never stands
    // whole. A late joiner takes the same path from the model snapshot. The rebuild waits for the next tick, so the
    // order in which the field's state and lists arrive does not matter; the root pose follows the model at once, so
    // a world shift moves the structures in the frame the ship moves.
    public sealed class LiveFieldBuildSystem : IInitializable, IDisposable, ITickable {
        private const float CELL_CENTER = 0.5f;

        private enum PlacementState {
            NotBuilt,
            Built,
            NotLocal,
        }

        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly FlightFieldSceneModel _flightFieldSceneModel;
        private readonly IFieldStructureSceneBuilder _fieldStructureSceneBuilder;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IFieldStreamingViewpoint _fieldStreamingViewpoint;
        private readonly List<PlacementState> _placementStates = new();

        private bool _isDirty;

        private bool IsShowingLiveField => _flightFieldSceneModel.BuiltNumber != LiveFieldNumbers.NO_FIELD && _flightFieldSceneModel.BuiltNumber == _liveFieldModel.Number;

        public LiveFieldBuildSystem(IReadOnlyLiveFieldModel liveFieldModel, FlightFieldSceneModel flightFieldSceneModel, IFieldStructureSceneBuilder fieldStructureSceneBuilder,
                                    FlightFieldConfiguration flightFieldConfiguration, IFieldStreamingViewpoint fieldStreamingViewpoint) {
            _liveFieldModel = liveFieldModel;
            _flightFieldSceneModel = flightFieldSceneModel;
            _fieldStructureSceneBuilder = fieldStructureSceneBuilder;
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldStreamingViewpoint = fieldStreamingViewpoint;
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

            StreamStructures();
        }

        private void FollowLiveFieldNumber() {
            int liveNumber = _liveFieldModel.IsAvailable ? _liveFieldModel.Number : LiveFieldNumbers.NO_FIELD;
            if (liveNumber == _flightFieldSceneModel.BuiltNumber)
                return;

            _fieldStructureSceneBuilder.ReleaseAll();
            _flightFieldSceneModel.Clear();
            _placementStates.Clear();
            if (liveNumber == LiveFieldNumbers.NO_FIELD)
                return;

            if (!_flightFieldSceneModel.HasRoot)
                _flightFieldSceneModel.SetRoot(_fieldStructureSceneBuilder.CreateRoot());

            _fieldStructureSceneBuilder.PlaceRoot(_flightFieldSceneModel.Root, _liveFieldModel);
            _flightFieldSceneModel.BeginBuild(liveNumber);
        }

        // The field counts as built once a pass ends with nothing left to build within the build radius.
        private void StreamStructures() {
            if (_flightFieldSceneModel.BuiltNumber == LiveFieldNumbers.NO_FIELD)
                return;

            IReadOnlyList<FieldPlacementEntry> placements = _liveFieldModel.Placements;
            while (_placementStates.Count < placements.Count)
                _placementStates.Add(PlacementState.NotBuilt);

            Vector3 viewpoint = _flightFieldSceneModel.Root.InverseTransformPoint(_fieldStreamingViewpoint.GetPosition());
            Vector2 viewpointOnPlane = new(viewpoint.x, viewpoint.z);
            float buildRadiusSquared = _flightFieldConfiguration.BuildRadius * _flightFieldConfiguration.BuildRadius;
            float releaseRadiusSquared = _flightFieldConfiguration.ReleaseRadius * _flightFieldConfiguration.ReleaseRadius;
            int buildBudget = _flightFieldConfiguration.StructuresBuiltPerFrame;
            bool isWaitingForBudget = false;
            for (int i = 0; i < placements.Count; i++) {
                PlacementState state = _placementStates[i];
                if (state == PlacementState.NotLocal)
                    continue;

                float distanceSquared = (PlacementOnPlane(placements[i]) - viewpointOnPlane).sqrMagnitude;
                if (state == PlacementState.Built && distanceSquared > releaseRadiusSquared) {
                    _fieldStructureSceneBuilder.ReleaseLocalStructure(_flightFieldSceneModel.Root, i);
                    _placementStates[i] = PlacementState.NotBuilt;
                }
                else if (state == PlacementState.NotBuilt && distanceSquared <= buildRadiusSquared) {
                    if (buildBudget == 0) {
                        isWaitingForBudget = true;
                        continue;
                    }

                    buildBudget--;
                    bool isBuilt = _fieldStructureSceneBuilder.BuildLocalStructure(_flightFieldSceneModel.Root, _liveFieldModel, i);
                    _placementStates[i] = isBuilt ? PlacementState.Built : PlacementState.NotLocal;
                }
            }

            if (!isWaitingForBudget && !_flightFieldSceneModel.IsBuilt)
                _flightFieldSceneModel.CompleteBuild();
        }

        // The center of the anchor cell in root space (x along Right, z along Forward); a structure of up to 4 cells
        // is off by at most two cells, small against the build radius.
        private Vector2 PlacementOnPlane(FieldPlacementEntry placement) {
            float cellSize = _liveFieldModel.CellSize;
            return new Vector2((placement.Anchor.y + CELL_CENTER) * cellSize, (placement.Anchor.x + CELL_CENTER) * cellSize);
        }

        private void OnLiveFieldChanged() {
            _isDirty = true;
            if (IsShowingLiveField)
                _fieldStructureSceneBuilder.PlaceRoot(_flightFieldSceneModel.Root, _liveFieldModel);
        }
    }
}
