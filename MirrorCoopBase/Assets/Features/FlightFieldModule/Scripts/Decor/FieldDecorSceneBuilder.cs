using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // Builds the decor under its own root, posed like the field root, so a world shift moves it the same way. Every
    // object comes from the decor's own pool (the field's pool is emptied whenever the field changes) and is
    // remembered per chunk; nothing built here has a collider.
    public sealed class FieldDecorSceneBuilder : IFieldDecorSceneBuilder {
        private const string ROOT_NAME = "FlightFieldDecor";

        private readonly IFieldDecorChunkLayout _fieldDecorChunkLayout;
        private readonly IFieldDecorPlanetPicker _fieldDecorPlanetPicker;
        private readonly IFieldStructurePool _fieldStructurePool;
        private readonly IFieldDecorLightSource _fieldDecorLightSource;
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        // One reseeded sequence and reused lists: chunks are built while flying and allocate nothing once warm.
        private readonly IFieldRandom _decorRandom;
        private readonly List<FieldClusterRockPose> _chunkRocks = new();
        private readonly List<FieldDecorPlanet> _planets = new();
        private readonly Dictionary<Vector2Int, List<GameObject>> _objectsByChunk = new();
        private readonly Stack<List<GameObject>> _freeObjectLists = new();

        public FieldDecorSceneBuilder(IFieldDecorChunkLayout fieldDecorChunkLayout, IFieldDecorPlanetPicker fieldDecorPlanetPicker, IFieldStructurePool fieldStructurePool,
                                      IFieldDecorLightSource fieldDecorLightSource, IReadOnlyLiveFieldModel liveFieldModel, IFieldRandomFactory fieldRandomFactory) {
            _fieldDecorChunkLayout = fieldDecorChunkLayout;
            _fieldDecorPlanetPicker = fieldDecorPlanetPicker;
            _fieldStructurePool = fieldStructurePool;
            _fieldDecorLightSource = fieldDecorLightSource;
            _liveFieldModel = liveFieldModel;
            _decorRandom = fieldRandomFactory.Create(0);
        }

        // Kept across scene loads with its pooled objects; it is empty whenever no field stands.
        public Transform CreateRoot() {
            GameObject root = new(ROOT_NAME);
            Object.DontDestroyOnLoad(root);
            return root.transform;
        }

        public void BuildChunk(Transform root, int fieldSeed, Vector2Int chunk) {
            List<GameObject> objects = TakeObjectList();
            _objectsByChunk.Add(chunk, objects);
            _fieldDecorChunkLayout.Fill(fieldSeed, chunk, _decorRandom, _chunkRocks);
            foreach (FieldClusterRockPose rockPose in _chunkRocks) {
                FieldRockVisual rock = _fieldStructurePool.TakeRock(rockPose.Prefab, root);
                objects.Add(rock.gameObject);
                rock.transform.SetLocalPositionAndRotation(rockPose.Offset, rockPose.Rotation);
                rock.transform.localScale = rockPose.Scale;
                rock.SetMaterial(rockPose.Material);
                rock.gameObject.SetActive(true);
            }
        }

        public void ReleaseChunk(Vector2Int chunk) {
            if (!_objectsByChunk.Remove(chunk, out List<GameObject> objects))
                return;

            foreach (GameObject builtObject in objects)
                _fieldStructurePool.Return(builtObject);

            objects.Clear();
            _freeObjectLists.Push(objects);
        }

        // The planets stand around the route axis (the middle of the field's width, the root is its corner) and turn
        // within their side to where the light comes from behind the viewer, so a player sees their lit side; with no
        // main light they follow the seed only.
        public void BuildPlanets(Transform root, int fieldSeed) {
            Vector3 courseStart = new(_liveFieldModel.WidthCells * _liveFieldModel.CellSize / 2f, 0f, 0f);
            if (_fieldDecorLightSource.TryGetLightTravel(out Vector3 lightTravel))
                _fieldDecorPlanetPicker.Fill(fieldSeed, courseStart, root.InverseTransformDirection(lightTravel), _planets);
            else
                _fieldDecorPlanetPicker.Fill(fieldSeed, courseStart, _planets);

            foreach (FieldDecorPlanet planet in _planets) {
                GameObject instance = _fieldStructurePool.Take(planet.Prefab, root);
                instance.transform.SetLocalPositionAndRotation(planet.Position, planet.Rotation);
                instance.transform.localScale = Vector3.one * planet.Diameter;
                instance.SetActive(true);
            }
        }

        public void ReleaseAll() {
            _fieldStructurePool.ReturnAll();
            foreach (List<GameObject> objects in _objectsByChunk.Values) {
                objects.Clear();
                _freeObjectLists.Push(objects);
            }

            _objectsByChunk.Clear();
        }

        private List<GameObject> TakeObjectList() =>
            _freeObjectLists.Count > 0 ? _freeObjectLists.Pop() : new List<GameObject>();
    }
}
