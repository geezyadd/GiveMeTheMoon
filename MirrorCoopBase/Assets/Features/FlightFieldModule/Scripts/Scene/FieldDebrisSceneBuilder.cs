using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Debris;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // Builds the small rocks of debris cells locally on every peer: where they stand comes from IFieldDebrisLayout
    // (field seed + cell), how they look (rock, material, turn, stretch) from a second sequence of the same cell, so
    // every peer shows the same debris without a network object per rock. The rocks come from the debris pool and
    // stand under their own root, posed like the field root.
    public sealed class FieldDebrisSceneBuilder : IFieldDebrisSceneBuilder {
        private const string ROOT_NAME = "FlightFieldDebris";
        private const float HALF = 0.5f;
        private const float CELL_CENTER = 0.5f;
        // The farthest vertex of a rock prefab from its pivot, as a share of the rock's size (see FieldClusterLayout).
        private const float ROCK_REACH = 0.7f;

        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly IFieldDebrisLayout _fieldDebrisLayout;
        private readonly IFieldStructurePool _fieldStructurePool;
        // Reused: debris is built every frame in flight and allocates nothing once the pool is warm.
        private readonly IFieldRandom _layoutRandom;
        private readonly IFieldRandom _lookRandom;
        private readonly List<FieldDebrisSpot> _spots = new();
        private readonly Dictionary<Vector2Int, List<GameObject>> _objectsByCell = new();
        private readonly Stack<List<GameObject>> _freeObjectLists = new();

        public FieldDebrisSceneBuilder(FieldStructureCatalog fieldStructureCatalog, FlightFieldConfiguration flightFieldConfiguration, IFieldDebrisLayout fieldDebrisLayout,
                                       IFieldStructurePool fieldStructurePool, IFieldRandomFactory fieldRandomFactory) {
            _fieldStructureCatalog = fieldStructureCatalog;
            _flightFieldConfiguration = flightFieldConfiguration;
            _fieldDebrisLayout = fieldDebrisLayout;
            _fieldStructurePool = fieldStructurePool;
            _layoutRandom = fieldRandomFactory.Create(0);
            _lookRandom = fieldRandomFactory.Create(0);
        }

        // Kept across scene loads with its pooled rocks; it is empty whenever no field stands.
        public Transform CreateRoot() {
            GameObject root = new(ROOT_NAME);
            Object.DontDestroyOnLoad(root);
            return root.transform;
        }

        public void BuildCell(Transform root, int fieldSeed, float cellSize, Vector2Int cell, float density, string debrisId) {
            FieldDebrisRecipe recipe = _fieldStructureCatalog.GetRequiredDebris(debrisId).Recipe;
            _fieldDebrisLayout.Fill(recipe, density, fieldSeed, cell, cellSize, _flightFieldConfiguration.DebrisMinGapMeters, _layoutRandom, _spots);
            if (_spots.Count == 0)
                return;

            List<GameObject> objects = _freeObjectLists.Count > 0 ? _freeObjectLists.Pop() : new List<GameObject>();
            _objectsByCell.Add(cell, objects);
            _lookRandom.Reseed(_fieldDebrisLayout.LookSeed(fieldSeed, cell));
            Vector3 cellCenter = new((cell.y + CELL_CENTER) * cellSize, 0f, (cell.x + CELL_CENTER) * cellSize);
            foreach (FieldDebrisSpot spot in _spots)
                objects.Add(BuildRock(root, recipe, cellCenter + spot.Offset, spot.Diameter));
        }

        public void ReleaseCell(Vector2Int cell) {
            if (!_objectsByCell.Remove(cell, out List<GameObject> objects))
                return;

            foreach (GameObject rock in objects)
                _fieldStructurePool.Return(rock);

            objects.Clear();
            _freeObjectLists.Push(objects);
        }

        public void ReleaseAll() {
            _fieldStructurePool.ReturnAll();
            foreach (List<GameObject> objects in _objectsByCell.Values) {
                objects.Clear();
                _freeObjectLists.Push(objects);
            }

            _objectsByCell.Clear();
        }

        // The diameter bounds the rock however it is turned: its size keeps the farthest vertex within the radius at
        // the largest axis stretch.
        private GameObject BuildRock(Transform root, FieldDebrisRecipe recipe, Vector3 position, float diameter) {
            FieldRockVisual rock = _fieldStructurePool.TakeRock(FieldClusterLayout.PickRock(recipe.Rocks, _lookRandom), root);
            float size = diameter * HALF / (ROCK_REACH * recipe.AxisScale.y);
            rock.transform.SetLocalPositionAndRotation(position, FieldClusterLayout.RandomRotation(_lookRandom));
            rock.transform.localScale = new Vector3(AxisScale(recipe) * size, AxisScale(recipe) * size, AxisScale(recipe) * size);
            rock.SetMaterial(recipe.Materials[_lookRandom.Range(0, recipe.Materials.Count)]);
            rock.gameObject.SetActive(true);
            return rock.gameObject;
        }

        private float AxisScale(FieldDebrisRecipe recipe) =>
            Mathf.Lerp(recipe.AxisScale.x, recipe.AxisScale.y, _lookRandom.Value());
    }
}
