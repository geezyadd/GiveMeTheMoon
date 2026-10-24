using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // Builds the rocks of debris cells locally on every peer: where they stand comes from IFieldDebrisLayout (field
    // seed + the cells around), how they look (rock, material, turn, stretch) from each rock's own look seed, so every
    // peer shows the same debris without a network object per rock. The rocks come from the debris pool and
    // stand under their own root, posed like the field root.
    public sealed class FieldDebrisSceneBuilder : IFieldDebrisSceneBuilder {
        private const string ROOT_NAME = "FlightFieldDebris";
        private const float HALF = 0.5f;

        private readonly IFieldDebrisLayout _fieldDebrisLayout;
        private readonly IFieldStructurePool _fieldStructurePool;
        private readonly IFieldBrokenRocks _fieldBrokenRocks;
        // Reused: debris is built every frame in flight and allocates nothing once the pool is warm.
        private readonly FieldDebrisWorkspace _workspace;
        private readonly IFieldRandom _lookRandom;
        private readonly List<FieldDebrisSpot> _spots = new();
        private readonly Dictionary<Vector2Int, FieldBuiltObjects> _objectsByCell = new();
        private readonly Stack<FieldBuiltObjects> _freeObjects = new();

        public FieldDebrisSceneBuilder(IFieldDebrisLayout fieldDebrisLayout, IFieldStructurePool fieldStructurePool, IFieldRandomFactory fieldRandomFactory,
                                       IFieldBrokenRocks fieldBrokenRocks) {
            _fieldDebrisLayout = fieldDebrisLayout;
            _fieldStructurePool = fieldStructurePool;
            _fieldBrokenRocks = fieldBrokenRocks;
            _workspace = new FieldDebrisWorkspace(fieldRandomFactory.Create(0));
            _lookRandom = fieldRandomFactory.Create(0);
        }

        // Kept across scene loads with its pooled rocks; it is empty whenever no field stands.
        public Transform CreateRoot() {
            GameObject root = new(ROOT_NAME);
            Object.DontDestroyOnLoad(root);
            return root.transform;
        }

        public void BuildCell(Transform root, int fieldSeed, float cellSize, Vector2Int cell, IFieldDebrisCells cells) {
            if (!cells.TryGetDebris(cell, out _, out FieldDebrisRecipe recipe))
                return;

            _fieldDebrisLayout.Fill(fieldSeed, cell, cellSize, cells, _workspace, _spots);
            if (_spots.Count == 0)
                return;

            FieldBuiltObjects objects = _freeObjects.Count > 0 ? _freeObjects.Pop() : new FieldBuiltObjects();
            _objectsByCell.Add(cell, objects);
            Vector3 cellCenter = FieldFrame.CellCenterInField(cell, cellSize);
            for (int rockIndex = 0; rockIndex < _spots.Count; rockIndex++) {
                FieldDebrisSpot spot = _spots[rockIndex];
                _lookRandom.Reseed(spot.LookSeed);
                FieldRockVisual rock = BuildRock(root, recipe, cellCenter + spot.Offset, spot.Diameter);
                objects.AddRock(rock);
                // A broken rock keeps its slot, so every rock index still names its rock, but stays hidden.
                rock.gameObject.SetActive(!_fieldBrokenRocks.IsBroken(FieldRockId.CreateDebris(cell, rockIndex)));
            }
        }

        public bool TryGetBuiltRock(Vector2Int cell, int rockIndex, out FieldRockVisual rock) {
            rock = null;
            if (!_objectsByCell.TryGetValue(cell, out FieldBuiltObjects objects) || rockIndex >= objects.Rocks.Count)
                return false;

            rock = objects.Rocks[rockIndex];
            return true;
        }

        public void ReleaseCell(Vector2Int cell) {
            if (!_objectsByCell.Remove(cell, out FieldBuiltObjects objects))
                return;

            foreach (GameObject rock in objects.Objects)
                _fieldStructurePool.Return(rock);

            objects.Clear();
            _freeObjects.Push(objects);
        }

        public void ReleaseAll() {
            _fieldStructurePool.ReturnAll();
            _workspace.Clear();
            foreach (FieldBuiltObjects objects in _objectsByCell.Values) {
                objects.Clear();
                _freeObjects.Push(objects);
            }

            _objectsByCell.Clear();
        }

        // The diameter bounds the rock however it is turned: its size keeps the farthest vertex within the radius at
        // the largest axis stretch.
        private FieldRockVisual BuildRock(Transform root, FieldDebrisRecipe recipe, Vector3 position, float diameter) {
            FieldRockVisual rock = _fieldStructurePool.Take(FieldClusterLayout.PickRock(recipe.Rocks, _lookRandom), root);
            float size = diameter * HALF / (FieldRockVisual.MESH_REACH * recipe.AxisScale.y);
            rock.transform.SetLocalPositionAndRotation(position, FieldClusterLayout.RandomRotation(_lookRandom));
            rock.transform.localScale = new Vector3(AxisScale(recipe) * size, AxisScale(recipe) * size, AxisScale(recipe) * size);
            rock.SetMaterial(recipe.Materials[_lookRandom.Range(0, recipe.Materials.Count)]);
            return rock;
        }

        private float AxisScale(FieldDebrisRecipe recipe) =>
            Mathf.Lerp(recipe.AxisScale.x, recipe.AxisScale.y, _lookRandom.Value());
    }
}
