using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.GameCoreModule.Contracts;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Scene {
    // Keeps this peer's local structures equal to the live field model around the viewpoint: a new field number
    // releases the old structures to the pool; then every frame the placements within the build radius are built, a
    // few per frame, and the built ones beyond the release radius go back to the pool, so a long field never stands
    // whole. A late joiner takes the same path from the model snapshot. The rebuild waits for the next tick, so the
    // order in which the field's state and lists arrive does not matter; the root pose follows the model at once, so
    // a world shift moves the structures in the frame the ship moves. The small debris of passable cells streams the
    // same way through IFieldDebrisStreamer under its own root.
    public sealed class LiveFieldBuildSystem : IInitializable, IDisposable, ITickable {
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly FlightFieldSceneModel _flightFieldSceneModel;
        private readonly IFieldStructureSceneBuilder _fieldStructureSceneBuilder;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly ILocalViewpointSource _localViewpointSource;
        private readonly IFieldStreamingRule _fieldStreamingRule;
        private readonly IFieldDebrisSceneBuilder _fieldDebrisSceneBuilder;
        private readonly IFieldDebrisStreamer _fieldDebrisStreamer;

        private bool _isDirty;

        private bool IsShowingLiveField => _flightFieldSceneModel.BuiltNumber != LiveFieldNumbers.NO_FIELD && _flightFieldSceneModel.BuiltNumber == _liveFieldModel.Number;

        public LiveFieldBuildSystem(IReadOnlyLiveFieldModel liveFieldModel, FlightFieldSceneModel flightFieldSceneModel, IFieldStructureSceneBuilder fieldStructureSceneBuilder,
                                    FlightFieldConfiguration flightFieldConfiguration, ILocalViewpointSource localViewpointSource, IFieldStreamingRule fieldStreamingRule,
                                    IFieldDebrisSceneBuilder fieldDebrisSceneBuilder, IFieldDebrisStreamer fieldDebrisStreamer) {
            _liveFieldModel = liveFieldModel;
            _flightFieldSceneModel = flightFieldSceneModel;
            _fieldStructureSceneBuilder = fieldStructureSceneBuilder;
            _flightFieldConfiguration = flightFieldConfiguration;
            _localViewpointSource = localViewpointSource;
            _fieldStreamingRule = fieldStreamingRule;
            _fieldDebrisSceneBuilder = fieldDebrisSceneBuilder;
            _fieldDebrisStreamer = fieldDebrisStreamer;
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
            _fieldDebrisStreamer.Clear();
            _flightFieldSceneModel.Clear();
            if (liveNumber == LiveFieldNumbers.NO_FIELD)
                return;

            if (!_flightFieldSceneModel.HasRoot)
                _flightFieldSceneModel.SetRoots(_fieldStructureSceneBuilder.CreateRoot(), _fieldDebrisSceneBuilder.CreateRoot());

            PlaceRoots();
            _fieldDebrisStreamer.Restart(_liveFieldModel);
            _flightFieldSceneModel.BeginBuild(liveNumber);
        }

        // The field counts as built once a pass ends with nothing left to build within the build radius.
        private void StreamStructures() {
            if (_flightFieldSceneModel.BuiltNumber == LiveFieldNumbers.NO_FIELD)
                return;

            IReadOnlyList<FieldPlacementEntry> placements = _liveFieldModel.Placements;
            _flightFieldSceneModel.TrackPlacements(placements.Count);
            FieldFrame frame = _flightFieldSceneModel.Frame;
            Vector2 viewpointOnPlane = frame.WorldToPlane(_localViewpointSource.GetPosition());
            int buildBudget = _flightFieldConfiguration.StructuresBuiltPerFrame;
            bool isWaitingForBudget = false;
            for (int i = 0; i < placements.Count; i++) {
                FieldPlacementState state = _flightFieldSceneModel.PlacementStates[i];
                if (state == FieldPlacementState.NotLocal)
                    continue;

                // A structure of up to 4 cells is measured from its anchor cell: off by at most two cells, small
                // against the build radius.
                float distanceSquared = (frame.CellToPlane(placements[i].Anchor) - viewpointOnPlane).sqrMagnitude;
                if (state == FieldPlacementState.Built && _fieldStreamingRule.ShouldRelease(distanceSquared)) {
                    ReleaseStructure(i);
                    continue;
                }

                if (state != FieldPlacementState.NotBuilt || !_fieldStreamingRule.ShouldBuild(distanceSquared))
                    continue;

                if (buildBudget == 0) {
                    isWaitingForBudget = true;
                    continue;
                }

                buildBudget--;
                BuildStructure(i);
            }

            if (!isWaitingForBudget && !_flightFieldSceneModel.IsBuilt)
                _flightFieldSceneModel.CompleteBuild();

            _fieldDebrisStreamer.Stream(_flightFieldSceneModel.DebrisRoot, _liveFieldModel, viewpointOnPlane);
        }

        private void BuildStructure(int placementIndex) {
            bool isBuilt = _fieldStructureSceneBuilder.BuildLocalStructure(_flightFieldSceneModel.Root, _liveFieldModel, placementIndex);
            _flightFieldSceneModel.SetPlacementState(placementIndex, isBuilt ? FieldPlacementState.Built : FieldPlacementState.NotLocal);
        }

        private void ReleaseStructure(int placementIndex) {
            _fieldStructureSceneBuilder.ReleaseLocalStructure(placementIndex);
            _flightFieldSceneModel.SetPlacementState(placementIndex, FieldPlacementState.NotBuilt);
        }

        private void PlaceRoots() {
            _fieldStructureSceneBuilder.PlaceRoot(_flightFieldSceneModel.Root, _liveFieldModel);
            _fieldStructureSceneBuilder.PlaceRoot(_flightFieldSceneModel.DebrisRoot, _liveFieldModel);
            _flightFieldSceneModel.SetFrame(new FieldFrame(_liveFieldModel.Origin, _liveFieldModel.Forward, _liveFieldModel.CellSize, _liveFieldModel.LengthCells,
                _liveFieldModel.WidthCells));
        }

        private void OnLiveFieldChanged() {
            _isDirty = true;
            if (IsShowingLiveField)
                PlaceRoots();
        }
    }
}
