using System;
using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Scene;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Features.FlightFieldModule.Scripts.Beacons {
    // Builds the beacons locally on every peer from the beacon prefab, from its own pool, under its own root posed
    // like the field root. Two colour materials, made once from the prefab's light material (the asset itself is never
    // changed), carry the main and the alternative colour; the blink changes only their colour, so all beacons of one
    // colour blink together for two material writes a frame.
    public sealed class FieldBeaconSceneBuilder : IFieldBeaconSceneBuilder {
        private const string ROOT_NAME = "FlightFieldBeacons";
        private const string MAIN_MATERIAL_NAME = "FieldBeacon_Main (runtime)";
        private const string ALTERNATIVE_MATERIAL_NAME = "FieldBeacon_Alternative (runtime)";
        private const float FULL_TURN = Mathf.PI * 2f;
        private const float HALF = 0.5f;

        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");

        private readonly IFieldStructurePool _fieldStructurePool;
        private readonly FlightFieldConfiguration _flightFieldConfiguration;
        private readonly Dictionary<int, GameObject> _objectsByIndex = new();

        private Material _mainMaterial;
        private Material _alternativeMaterial;

        private FieldBeaconConfiguration Configuration => _flightFieldConfiguration.Beacons;

        public FieldBeaconSceneBuilder(IFieldStructurePool fieldStructurePool, FlightFieldConfiguration flightFieldConfiguration) {
            _fieldStructurePool = fieldStructurePool;
            _flightFieldConfiguration = flightFieldConfiguration;
        }

        // Kept across scene loads with its pooled beacons; it is empty whenever no field stands.
        public Transform CreateRoot() {
            GameObject root = new(ROOT_NAME);
            Object.DontDestroyOnLoad(root);
            _mainMaterial = CreateColorMaterial(MAIN_MATERIAL_NAME);
            _alternativeMaterial = CreateColorMaterial(ALTERNATIVE_MATERIAL_NAME);
            return root.transform;
        }

        public void Build(Transform root, int index, FieldBeacon beacon) {
            FieldBeaconVisual visual = _fieldStructurePool.Take(Configuration.Prefab, root);
            visual.SetMaterial(MaterialOf(beacon.Lane));
            visual.transform.SetLocalPositionAndRotation(beacon.Position, Quaternion.identity);
            visual.gameObject.SetActive(true);
            _objectsByIndex.Add(index, visual.gameObject);
        }

        public void Release(int index) {
            if (_objectsByIndex.Remove(index, out GameObject instance))
                _fieldStructurePool.Return(instance);
        }

        public void ReleaseAll() {
            _fieldStructurePool.ReturnAll();
            _objectsByIndex.Clear();
        }

        public void ApplyBlink(float time) {
            float wave = HALF + HALF * Mathf.Cos(time / Configuration.BlinkPeriodSeconds * FULL_TURN);
            float intensity = Mathf.Lerp(Configuration.BlinkIntensity.x, Configuration.BlinkIntensity.y, wave);
            _mainMaterial.SetColor(_baseColorId, Configuration.MainColor * intensity);
            _alternativeMaterial.SetColor(_baseColorId, Configuration.AlternativeColor * intensity);
        }

        private Material CreateColorMaterial(string materialName) =>
            new(Configuration.Prefab.LightMaterial) { name = materialName };

        private Material MaterialOf(FieldBeaconLane lane) =>
            lane switch {
                FieldBeaconLane.Main => _mainMaterial,
                FieldBeaconLane.Alternative => _alternativeMaterial,
                _ => throw new ArgumentOutOfRangeException(nameof(lane), lane, "A beacon has no lane colour."),
            };
    }
}
