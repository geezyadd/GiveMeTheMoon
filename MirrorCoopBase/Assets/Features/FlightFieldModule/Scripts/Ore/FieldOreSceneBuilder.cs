using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Scene;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Takes a piece of its kind's prefab from the pool under the ore root and binds its weapon target id; a released
    // piece is unbound and goes back to the pool.
    public sealed class FieldOreSceneBuilder : IFieldOreSceneBuilder {
        private const string ROOT_NAME = "FlightFieldOre";

        private readonly FieldOreCatalog _fieldOreCatalog;
        private readonly FieldOreConfiguration _fieldOreConfiguration;
        private readonly IFieldOreWeaponIds _fieldOreWeaponIds;
        private readonly IFieldStructurePool _fieldStructurePool;

        public FieldOreSceneBuilder(FieldOreCatalog fieldOreCatalog, FieldOreConfiguration fieldOreConfiguration, IFieldOreWeaponIds fieldOreWeaponIds,
                                    IFieldStructurePool fieldStructurePool) {
            _fieldOreCatalog = fieldOreCatalog;
            _fieldOreConfiguration = fieldOreConfiguration;
            _fieldOreWeaponIds = fieldOreWeaponIds;
            _fieldStructurePool = fieldStructurePool;
        }

        public Transform CreateRoot() {
            GameObject root = new(ROOT_NAME);
            Object.DontDestroyOnLoad(root);
            return root.transform;
        }

        public FieldOreView Build(Transform root, in FieldOreEntry entry) {
            FieldOreView view = _fieldStructurePool.Take(_fieldOreCatalog.GetRequired(entry.KindId).Prefab, root);
            view.Bind(_fieldOreWeaponIds.GetTargetId(new FieldOreId(entry.Id)), _fieldOreConfiguration.SpinDegreesPerSecond);
            view.gameObject.SetActive(true);
            return view;
        }

        public void Release(FieldOreView view) {
            view.Unbind();
            _fieldStructurePool.Return(view.gameObject);
        }
    }
}
