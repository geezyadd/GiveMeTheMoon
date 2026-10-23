using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Scene;
using Features.GameCoreModule.Contracts;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Decor {
    // Keeps this peer's field decor around its ship: a new field releases the old decor and places its planets; then
    // the chunks within the view distance of the ship are built a few per frame and the ones left behind are
    // released. The decor root follows the live field's pose at once, so a world shift moves it with the field.
    public sealed class FieldDecorBuildSystem : IInitializable, IDisposable, ITickable {
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly FieldDecorSceneModel _fieldDecorSceneModel;
        private readonly IFieldDecorSceneBuilder _fieldDecorSceneBuilder;
        private readonly IFieldStructureSceneBuilder _fieldStructureSceneBuilder;
        private readonly IShipPositionSource _shipPositionSource;
        private readonly FlightFieldDecorConfiguration _flightFieldDecorConfiguration;
        private readonly List<Vector2Int> _chunksToRelease = new();

        private bool IsShowingLiveField => _fieldDecorSceneModel.BuiltNumber != LiveFieldNumbers.NO_FIELD && _fieldDecorSceneModel.BuiltNumber == _liveFieldModel.Number;

        public FieldDecorBuildSystem(IReadOnlyLiveFieldModel liveFieldModel, FieldDecorSceneModel fieldDecorSceneModel, IFieldDecorSceneBuilder fieldDecorSceneBuilder,
                                     IFieldStructureSceneBuilder fieldStructureSceneBuilder, IShipPositionSource shipPositionSource,
                                     FlightFieldDecorConfiguration flightFieldDecorConfiguration) {
            _liveFieldModel = liveFieldModel;
            _fieldDecorSceneModel = fieldDecorSceneModel;
            _fieldDecorSceneBuilder = fieldDecorSceneBuilder;
            _fieldStructureSceneBuilder = fieldStructureSceneBuilder;
            _shipPositionSource = shipPositionSource;
            _flightFieldDecorConfiguration = flightFieldDecorConfiguration;
        }

        public void Initialize() =>
            _liveFieldModel.OnChanged += OnLiveFieldChanged;

        public void Dispose() =>
            _liveFieldModel.OnChanged -= OnLiveFieldChanged;

        public void Tick() {
            FollowLiveFieldNumber();
            if (_fieldDecorSceneModel.BuiltNumber == LiveFieldNumbers.NO_FIELD || !_shipPositionSource.TryGetShipPosition(out Vector3 shipPosition))
                return;

            Vector3 fieldPosition = _fieldDecorSceneModel.Root.InverseTransformPoint(shipPosition);
            Vector2 shipOnPlane = new(fieldPosition.x, fieldPosition.z);
            ReleaseFarChunks(shipOnPlane);
            BuildNearChunks(shipOnPlane);
        }

        private void FollowLiveFieldNumber() {
            int liveNumber = _liveFieldModel.IsAvailable ? _liveFieldModel.Number : LiveFieldNumbers.NO_FIELD;
            if (liveNumber == _fieldDecorSceneModel.BuiltNumber)
                return;

            _fieldDecorSceneBuilder.ReleaseAll();
            _fieldDecorSceneModel.Clear();
            if (liveNumber == LiveFieldNumbers.NO_FIELD)
                return;

            if (!_fieldDecorSceneModel.HasRoot)
                _fieldDecorSceneModel.SetRoot(_fieldDecorSceneBuilder.CreateRoot());

            _fieldStructureSceneBuilder.PlaceRoot(_fieldDecorSceneModel.Root, _liveFieldModel);
            _fieldDecorSceneModel.BeginBuild(liveNumber);
            _fieldDecorSceneBuilder.BuildPlanets(_fieldDecorSceneModel.Root, _liveFieldModel.Seed);
        }

        private void ReleaseFarChunks(Vector2 shipOnPlane) {
            _chunksToRelease.Clear();
            foreach (Vector2Int chunk in _fieldDecorSceneModel.BuiltChunks) {
                if (!IsInView(chunk, shipOnPlane))
                    _chunksToRelease.Add(chunk);
            }

            foreach (Vector2Int chunk in _chunksToRelease) {
                _fieldDecorSceneBuilder.ReleaseChunk(chunk);
                _fieldDecorSceneModel.RemoveChunk(chunk);
            }
        }

        private void BuildNearChunks(Vector2 shipOnPlane) {
            float chunkSize = _flightFieldDecorConfiguration.ChunkSize;
            int reach = Mathf.CeilToInt(_flightFieldDecorConfiguration.ViewDistance / chunkSize);
            Vector2Int shipChunk = new(Mathf.FloorToInt(shipOnPlane.x / chunkSize), Mathf.FloorToInt(shipOnPlane.y / chunkSize));
            int budget = _flightFieldDecorConfiguration.ChunksBuiltPerFrame;
            for (int x = shipChunk.x - reach; x <= shipChunk.x + reach && budget > 0; x++) {
                for (int z = shipChunk.y - reach; z <= shipChunk.y + reach && budget > 0; z++)
                    budget -= TryBuildChunk(new Vector2Int(x, z), shipOnPlane) ? 1 : 0;
            }
        }

        private bool TryBuildChunk(Vector2Int chunk, Vector2 shipOnPlane) {
            if (_fieldDecorSceneModel.IsChunkBuilt(chunk) || !IsInView(chunk, shipOnPlane))
                return false;

            _fieldDecorSceneBuilder.BuildChunk(_fieldDecorSceneModel.Root, _liveFieldModel.Seed, chunk);
            _fieldDecorSceneModel.AddChunk(chunk);
            return true;
        }

        private bool IsInView(Vector2Int chunk, Vector2 shipOnPlane) {
            float chunkSize = _flightFieldDecorConfiguration.ChunkSize;
            Vector2 chunkMiddle = new((chunk.x + 0.5f) * chunkSize, (chunk.y + 0.5f) * chunkSize);
            return (chunkMiddle - shipOnPlane).sqrMagnitude <= _flightFieldDecorConfiguration.ViewDistance * _flightFieldDecorConfiguration.ViewDistance;
        }

        private void OnLiveFieldChanged() {
            if (IsShowingLiveField)
                _fieldStructureSceneBuilder.PlaceRoot(_fieldDecorSceneModel.Root, _liveFieldModel);
        }
    }
}
