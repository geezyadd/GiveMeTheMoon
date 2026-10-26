using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Scene;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Takes a balloon of its kind's prefab from the pool, puts it at its field position under the targets root,
    // scales it by the kind's radius and binds its weapon target id and its bob (a phase from the index, so the
    // balloons do not bob in step). A released balloon is unbound and goes back to the pool.
    public sealed class FieldTargetSceneBuilder : IFieldTargetSceneBuilder {
        private const string ROOT_NAME = "FlightFieldTargets";
        // An irrational step: neighbouring indices get phases far apart.
        private const float BOB_PHASE_STEP = 2.39996323f;

        private readonly FieldTargetCatalog _fieldTargetCatalog;
        private readonly FieldTargetConfiguration _fieldTargetConfiguration;
        private readonly IFieldTargetWeaponIds _fieldTargetWeaponIds;
        private readonly IFieldStructurePool _fieldStructurePool;

        public FieldTargetSceneBuilder(FieldTargetCatalog fieldTargetCatalog, FieldTargetConfiguration fieldTargetConfiguration, IFieldTargetWeaponIds fieldTargetWeaponIds,
                                       IFieldStructurePool fieldStructurePool) {
            _fieldTargetCatalog = fieldTargetCatalog;
            _fieldTargetConfiguration = fieldTargetConfiguration;
            _fieldTargetWeaponIds = fieldTargetWeaponIds;
            _fieldStructurePool = fieldStructurePool;
        }

        public Transform CreateRoot() {
            GameObject root = new(ROOT_NAME);
            Object.DontDestroyOnLoad(root);
            return root.transform;
        }

        public void PlaceRoot(Transform root, IReadOnlyLiveFieldModel liveFieldModel) =>
            root.SetPositionAndRotation(liveFieldModel.Origin, Quaternion.LookRotation(liveFieldModel.Forward, Vector3.up));

        public FieldTargetView Build(Transform root, FieldTargetEntry target, int index) {
            FieldTargetDefinition kind = _fieldTargetCatalog.GetRequired(target.KindId);
            FieldTargetView view = _fieldStructurePool.Take(kind.Prefab, root);
            view.transform.SetLocalPositionAndRotation(target.FieldPosition, Quaternion.identity);
            view.transform.localScale = Vector3.one * kind.RadiusMeters;
            view.Bind(_fieldTargetWeaponIds.GetTargetId(new FieldTargetId(index)), kind.RadiusMeters, _fieldTargetConfiguration.BobAmplitudeMeters,
                _fieldTargetConfiguration.BobPeriodSeconds, index * BOB_PHASE_STEP, _fieldTargetConfiguration.HitFeedbackSeconds, _fieldTargetConfiguration.HitSquashShare);
            view.gameObject.SetActive(true);
            return view;
        }

        public void Release(FieldTargetView view) {
            view.Unbind();
            _fieldStructurePool.Return(view.gameObject);
        }
    }
}
