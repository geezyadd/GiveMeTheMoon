using System;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Every peer: a balloon that joins the live field's popped list goes back to the pool if this peer has it built,
    // and its pop plays where it stood. A balloon that is not built here (far away, or a late joiner's snapshot) has
    // nothing to show; the build system never builds a popped one. The effects stand under the targets root, which
    // follows a world shift like the field root.
    public sealed class FieldTargetPopViewSystem : IInitializable, IDisposable {
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly FieldTargetSceneModel _fieldTargetSceneModel;
        private readonly IFieldTargetSceneBuilder _fieldTargetSceneBuilder;
        private readonly IFieldTargetPopEffectPool _fieldTargetPopEffectPool;
        private readonly FieldTargetCatalog _fieldTargetCatalog;

        public FieldTargetPopViewSystem(IReadOnlyLiveFieldModel liveFieldModel, FieldTargetSceneModel fieldTargetSceneModel, IFieldTargetSceneBuilder fieldTargetSceneBuilder,
                                        IFieldTargetPopEffectPool fieldTargetPopEffectPool, FieldTargetCatalog fieldTargetCatalog) {
            _liveFieldModel = liveFieldModel;
            _fieldTargetSceneModel = fieldTargetSceneModel;
            _fieldTargetSceneBuilder = fieldTargetSceneBuilder;
            _fieldTargetPopEffectPool = fieldTargetPopEffectPool;
            _fieldTargetCatalog = fieldTargetCatalog;
        }

        public void Initialize() =>
            _liveFieldModel.OnPoppedTargetsAdded += OnPoppedTargetAdded;

        public void Dispose() =>
            _liveFieldModel.OnPoppedTargetsAdded -= OnPoppedTargetAdded;

        private void OnPoppedTargetAdded(int index, FieldTargetId id) {
            bool isShowingLiveField = _fieldTargetSceneModel.HasRoot && _fieldTargetSceneModel.BuiltNumber == _liveFieldModel.Number;
            if (!isShowingLiveField || !_fieldTargetSceneModel.TryGetView(id.Index, out FieldTargetView view))
                return;

            FieldTargetDefinition kind = _fieldTargetCatalog.GetRequired(_liveFieldModel.Targets[id.Index].KindId);
            _fieldTargetPopEffectPool.Play(_fieldTargetSceneModel.Root, view.transform.position, kind);
            _fieldTargetSceneBuilder.Release(view);
            _fieldTargetSceneModel.SetView(id.Index, null);
        }
    }
}
