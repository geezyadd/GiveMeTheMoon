using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // Builds the local structures of the live field under one root posed at the field frame: the root follows the
    // frame origin, so a world shift moves every structure at once. A structure is its trigger prefab plus the rocks
    // of its cluster recipe, parented to the trigger; every object comes from the pool and is remembered by placement
    // index. The visual turn, scale and rocks come from the field seed and the placement index
    // (IFieldPlacementRockLayout), so every peer shows the same field.
    public sealed class FieldStructureSceneBuilder : IFieldStructureSceneBuilder {
        private const string ROOT_NAME = "FlightField";

        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldStructureSpawnRule _fieldStructureSpawnRule;
        private readonly IFieldStructureLayout _fieldStructureLayout;
        private readonly IFieldFootprintReader _fieldFootprintReader;
        private readonly IFieldPlacementRockLayout _fieldPlacementRockLayout;
        private readonly IFieldStructurePool _fieldStructurePool;
        private readonly IFieldBrokenRocks _fieldBrokenRocks;
        // Reused lists: building runs every frame of the takeoff and allocates nothing once the pool is warm.
        private readonly List<FieldClusterRockPose> _clusterRocks = new();
        private readonly Dictionary<int, FieldBuiltObjects> _objectsByPlacement = new();
        private readonly Stack<FieldBuiltObjects> _freeObjects = new();

        public FieldStructureSceneBuilder(FieldStructureCatalog fieldStructureCatalog, IFieldStructureSpawnRule fieldStructureSpawnRule, IFieldStructureLayout fieldStructureLayout,
                                          IFieldFootprintReader fieldFootprintReader, IFieldPlacementRockLayout fieldPlacementRockLayout, IFieldStructurePool fieldStructurePool,
                                          IFieldBrokenRocks fieldBrokenRocks) {
            _fieldStructureCatalog = fieldStructureCatalog;
            _fieldStructureSpawnRule = fieldStructureSpawnRule;
            _fieldStructureLayout = fieldStructureLayout;
            _fieldFootprintReader = fieldFootprintReader;
            _fieldPlacementRockLayout = fieldPlacementRockLayout;
            _fieldStructurePool = fieldStructurePool;
            _fieldBrokenRocks = fieldBrokenRocks;
        }

        // Kept across scene loads with its pooled objects; it is empty whenever no field stands.
        public Transform CreateRoot() {
            GameObject root = new(ROOT_NAME);
            Object.DontDestroyOnLoad(root);
            return root.transform;
        }

        public void PlaceRoot(Transform root, IReadOnlyLiveFieldModel liveFieldModel) =>
            root.SetPositionAndRotation(liveFieldModel.Origin, Quaternion.LookRotation(liveFieldModel.Forward, Vector3.up));

        // A networked placement is skipped (false): the server spawns it through Mirror.
        public bool BuildLocalStructure(Transform root, IReadOnlyLiveFieldModel liveFieldModel, int placementIndex) {
            FieldPlacementEntry placement = liveFieldModel.Placements[placementIndex];
            FieldStructureDefinition structure = _fieldStructureCatalog.GetRequired(placement.StructureId);
            if (!_fieldStructureSpawnRule.IsBuiltLocally(structure))
                return false;

            FieldBuiltObjects objects = _freeObjects.Count > 0 ? _freeObjects.Pop() : new FieldBuiltObjects();
            _objectsByPlacement.Add(placementIndex, objects);
            FieldFootprint footprint = _fieldFootprintReader.Read(structure, placement.Rotation);
            Pose pose = _fieldStructureLayout.GetFieldPose(footprint, placement.Anchor, placement.Rotation, liveFieldModel.CellSize);
            FieldStructureJitter jitter = _fieldPlacementRockLayout.Fill(liveFieldModel.Seed, placementIndex, structure, footprint, liveFieldModel.CellSize, _clusterRocks);
            Transform instance = BuildTrigger(root, structure, pose, jitter, objects);
            BuildCluster(instance, placementIndex, objects);
            return true;
        }

        public bool TryGetBuiltRock(int placementIndex, int rockIndex, out FieldRockVisual rock) {
            rock = null;
            if (!_objectsByPlacement.TryGetValue(placementIndex, out FieldBuiltObjects objects) || rockIndex >= objects.Rocks.Count)
                return false;

            rock = objects.Rocks[rockIndex];
            return true;
        }

        public void ReleaseLocalStructure(int placementIndex) {
            if (!_objectsByPlacement.Remove(placementIndex, out FieldBuiltObjects objects))
                return;

            foreach (GameObject builtObject in objects.Objects)
                _fieldStructurePool.Return(builtObject);

            objects.Clear();
            _freeObjects.Push(objects);
        }

        public void ReleaseAll() {
            _fieldStructurePool.ReturnAll();
            foreach (FieldBuiltObjects objects in _objectsByPlacement.Values) {
                objects.Clear();
                _freeObjects.Push(objects);
            }

            _objectsByPlacement.Clear();
        }

        private Transform BuildTrigger(Transform root, FieldStructureDefinition structure, Pose pose, FieldStructureJitter jitter, FieldBuiltObjects objects) {
            Transform instance = _fieldStructurePool.Take(structure.Prefab, root).transform;
            objects.AddObject(instance.gameObject);
            instance.SetLocalPositionAndRotation(pose.position, pose.rotation * Quaternion.Euler(0f, jitter.YawDegrees, 0f));
            instance.localScale = structure.Prefab.transform.localScale * jitter.Scale;
            instance.gameObject.SetActive(true);
            return instance;
        }

        // The rocks are children of the trigger, so the field root keeps one object per structure; their offsets
        // are in field space and are carried into the trigger's turned and scaled space. A broken rock still takes
        // its pooled object, so every rock index keeps its slot, but it stays hidden.
        private void BuildCluster(Transform trigger, int placementIndex, FieldBuiltObjects objects) {
            Quaternion toTrigger = Quaternion.Inverse(trigger.localRotation);
            float triggerScale = trigger.localScale.x;
            for (int rockIndex = 0; rockIndex < _clusterRocks.Count; rockIndex++) {
                FieldClusterRockPose rockPose = _clusterRocks[rockIndex];
                FieldRockVisual rock = _fieldStructurePool.Take(rockPose.Prefab, trigger);
                objects.AddRock(rock);
                rock.transform.SetLocalPositionAndRotation(toTrigger * rockPose.Offset / triggerScale, toTrigger * rockPose.Rotation);
                rock.transform.localScale = rockPose.Scale / triggerScale;
                rock.SetMaterial(rockPose.Material);
                rock.gameObject.SetActive(!_fieldBrokenRocks.IsBroken(FieldRockId.CreateCluster(placementIndex, rockIndex)));
            }
        }
    }
}
