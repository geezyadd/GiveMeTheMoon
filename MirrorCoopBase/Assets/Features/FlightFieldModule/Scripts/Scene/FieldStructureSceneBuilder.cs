using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Structures;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Scene {
    // Builds the local structures of the live field under one root posed at the field frame: the root follows the
    // frame origin, so a world shift moves every structure at once. The visual turn and scale come from the field seed
    // and the placement index, so every peer shows the same field.
    public sealed class FieldStructureSceneBuilder : IFieldStructureSceneBuilder {
        private const string ROOT_NAME = "FlightField";
        // A large prime keeps the visual sequences of neighbouring placements apart.
        private const int VISUAL_SEED_STEP = 486187739;

        private readonly FieldStructureCatalog _fieldStructureCatalog;
        private readonly IFieldStructureSpawnRule _fieldStructureSpawnRule;
        private readonly IFieldStructureLayout _fieldStructureLayout;
        private readonly IFieldStructurePool _fieldStructurePool;
        // One sequence reseeded per placement: building runs every frame of the takeoff and allocates nothing.
        private readonly IFieldRandom _visualRandom;
        // Placement index -> the object built for it, so one placement can go back to the pool on its own.
        private readonly Dictionary<int, GameObject> _builtByPlacement = new();

        public FieldStructureSceneBuilder(FieldStructureCatalog fieldStructureCatalog, IFieldStructureSpawnRule fieldStructureSpawnRule, IFieldStructureLayout fieldStructureLayout,
                                          IFieldStructurePool fieldStructurePool, IFieldRandomFactory fieldRandomFactory) {
            _fieldStructureCatalog = fieldStructureCatalog;
            _fieldStructureSpawnRule = fieldStructureSpawnRule;
            _fieldStructureLayout = fieldStructureLayout;
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
        public bool BuildLocalStructure(Transform root, IReadOnlyLiveFieldModel liveFieldModel, int placementIndex) {
            FieldPlacementEntry placement = liveFieldModel.Placements[placementIndex];
            FieldStructureDefinition structure = _fieldStructureCatalog.GetRequired(placement.StructureId);
            if (!_fieldStructureSpawnRule.IsBuiltLocally(structure))
                return false;

            Pose pose = _fieldStructureLayout.GetFieldPose(structure, placement.Anchor, placement.Rotation, liveFieldModel.CellSize);
            _visualRandom.Reseed(unchecked(liveFieldModel.Seed + placementIndex * VISUAL_SEED_STEP));
            float yaw = Mathf.Lerp(-structure.VisualYawJitterDegrees, structure.VisualYawJitterDegrees, _visualRandom.Value());
            float scale = 1f + Mathf.Lerp(-structure.VisualScaleJitter, structure.VisualScaleJitter, _visualRandom.Value());
            Transform instance = _fieldStructurePool.Take(structure.Prefab, root).transform;
            instance.SetLocalPositionAndRotation(pose.position, pose.rotation * Quaternion.Euler(0f, yaw, 0f));
            instance.localScale = structure.Prefab.transform.localScale * scale;
            instance.gameObject.SetActive(true);
            _builtByPlacement[placementIndex] = instance.gameObject;
            return true;
        }

        public void ReleaseLocalStructure(Transform root, int placementIndex) {
            if (_builtByPlacement.Remove(placementIndex, out GameObject instance))
                _fieldStructurePool.Return(instance);
        }

        public void ReleaseAll() {
            _builtByPlacement.Clear();
            _fieldStructurePool.ReturnAll();
        }
    }
}
