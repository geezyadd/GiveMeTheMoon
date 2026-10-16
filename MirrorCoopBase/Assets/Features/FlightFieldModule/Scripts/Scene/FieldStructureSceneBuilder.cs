using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // Builds the local structures of the live field under one root posed at the field frame: the root follows the
    // frame origin, so a world shift moves every structure at once. A structure is its trigger prefab plus the rocks
    // of its cluster recipe, parented to the trigger; every object comes from the pool and is remembered by placement
    // index. The visual turn, scale and rocks come from the field seed and the placement index, so every peer shows
    // the same field.
    public sealed class FieldStructureSceneBuilder : IFieldStructureSceneBuilder {
        private const string ROOT_NAME = "FlightField";
        // A large prime keeps the visual sequences of neighbouring placements apart.
        private const int VISUAL_SEED_STEP = 486187739;

        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldStructureSpawnRule _fieldStructureSpawnRule;
        private readonly IFieldStructureLayout _fieldStructureLayout;
        private readonly IFieldFootprintReader _fieldFootprintReader;
        private readonly IFieldClusterLayout _fieldClusterLayout;
        private readonly IFieldStructurePool _fieldStructurePool;
        // One sequence reseeded per placement and reused lists: building runs every frame of the takeoff and
        // allocates nothing once the pool is warm.
        private readonly IFieldRandom _visualRandom;
        private readonly List<FieldClusterRockPose> _clusterRocks = new();
        private readonly Dictionary<int, List<GameObject>> _objectsByPlacement = new();
        private readonly Stack<List<GameObject>> _freeObjectLists = new();

        public FieldStructureSceneBuilder(FieldStructureCatalog fieldStructureCatalog, IFieldStructureSpawnRule fieldStructureSpawnRule, IFieldStructureLayout fieldStructureLayout,
                                          IFieldFootprintReader fieldFootprintReader, IFieldClusterLayout fieldClusterLayout, IFieldStructurePool fieldStructurePool,
                                          IFieldRandomFactory fieldRandomFactory) {
            _fieldStructureCatalog = fieldStructureCatalog;
            _fieldStructureSpawnRule = fieldStructureSpawnRule;
            _fieldStructureLayout = fieldStructureLayout;
            _fieldFootprintReader = fieldFootprintReader;
            _fieldClusterLayout = fieldClusterLayout;
            _fieldStructurePool = fieldStructurePool;
            _visualRandom = fieldRandomFactory.Create(0);
        }

        // Kept across scene loads with its pooled objects; it is empty whenever no field stands.
        public Transform CreateRoot() {
            GameObject root = new(ROOT_NAME);
            Object.DontDestroyOnLoad(root);
            return root.transform;
        }

        public void PlaceRoot(Transform root, IReadOnlyLiveFieldModel liveFieldModel) =>
            root.SetPositionAndRotation(liveFieldModel.Origin, Quaternion.LookRotation(liveFieldModel.Forward, Vector3.up));

        // A networked placement is skipped: the server spawns it through Mirror.
        public void BuildLocalStructure(Transform root, IReadOnlyLiveFieldModel liveFieldModel, int placementIndex) {
            FieldPlacementEntry placement = liveFieldModel.Placements[placementIndex];
            FieldStructureDefinition structure = _fieldStructureCatalog.GetRequired(placement.StructureId);
            if (!_fieldStructureSpawnRule.IsBuiltLocally(structure))
                return;

            List<GameObject> objects = TakeObjectList();
            _objectsByPlacement.Add(placementIndex, objects);
            Pose pose = _fieldStructureLayout.GetFieldPose(structure, placement.Anchor, placement.Rotation, liveFieldModel.CellSize);
            _visualRandom.Reseed(unchecked(liveFieldModel.Seed + placementIndex * VISUAL_SEED_STEP));
            Transform instance = BuildTrigger(root, structure, pose, objects);
            if (structure.HasClusterRecipe)
                BuildCluster(instance, structure, placement, liveFieldModel.CellSize, objects);
        }

        public void ReleaseLocalStructure(Transform root, int placementIndex) {
            if (!_objectsByPlacement.Remove(placementIndex, out List<GameObject> objects))
                return;

            foreach (GameObject builtObject in objects)
                _fieldStructurePool.Return(builtObject);

            objects.Clear();
            _freeObjectLists.Push(objects);
        }

        public void ReleaseAll() {
            _fieldStructurePool.ReturnAll();
            foreach (List<GameObject> objects in _objectsByPlacement.Values) {
                objects.Clear();
                _freeObjectLists.Push(objects);
            }

            _objectsByPlacement.Clear();
        }

        private Transform BuildTrigger(Transform root, FieldStructureDefinition structure, Pose pose, List<GameObject> objects) {
            float yaw = Mathf.Lerp(-structure.VisualYawJitterDegrees, structure.VisualYawJitterDegrees, _visualRandom.Value());
            float scale = 1f + Mathf.Lerp(-structure.VisualScaleJitter, structure.VisualScaleJitter, _visualRandom.Value());
            Transform instance = Take(structure.Prefab, root, objects).transform;
            instance.SetLocalPositionAndRotation(pose.position, pose.rotation * Quaternion.Euler(0f, yaw, 0f));
            instance.localScale = structure.Prefab.transform.localScale * scale;
            instance.gameObject.SetActive(true);
            return instance;
        }

        // The rocks are children of the trigger, so the field root keeps one object per structure; their offsets
        // are in field space and are carried into the trigger's turned and scaled space.
        private void BuildCluster(Transform trigger, FieldStructureDefinition structure, FieldPlacementEntry placement, float cellSize, List<GameObject> objects) {
            FieldFootprint footprint = _fieldFootprintReader.Read(structure, placement.Rotation);
            _fieldClusterLayout.Fill(structure.ClusterRecipe, footprint, cellSize, _visualRandom, _clusterRocks);
            Quaternion toTrigger = Quaternion.Inverse(trigger.localRotation);
            float triggerScale = trigger.localScale.x;
            foreach (FieldClusterRockPose rockPose in _clusterRocks) {
                GameObject rock = Take(rockPose.Prefab.gameObject, trigger, objects);
                rock.transform.SetParent(trigger, false);
                rock.transform.SetLocalPositionAndRotation(toTrigger * rockPose.Offset / triggerScale, toTrigger * rockPose.Rotation);
                rock.transform.localScale = rockPose.Scale / triggerScale;
                rock.GetComponent<FieldRockVisual>().SetMaterial(rockPose.Material);
                rock.SetActive(true);
            }
        }

        private GameObject Take(GameObject prefab, Transform parent, List<GameObject> objects) {
            GameObject instance = _fieldStructurePool.Take(prefab, parent);
            objects.Add(instance);
            return instance;
        }

        private List<GameObject> TakeObjectList() =>
            _freeObjectLists.Count > 0 ? _freeObjectLists.Pop() : new List<GameObject>();
    }
}
