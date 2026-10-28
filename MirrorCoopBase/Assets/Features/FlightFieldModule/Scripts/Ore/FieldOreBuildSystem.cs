using Features.FlightFieldModule.Scripts.Network;
using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Contracts;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Every peer, every frame: keeps this peer's ore views equal to the synced list. A new piece is built, every piece
    // is put where it is now and dimmed as it fades, a pulled one draws its rope from its hooker's weapon (when this
    // peer sees that player hold it), and a piece that left the list (taken in, gone, the field changed) goes back to
    // the pool. Nothing here goes over the network: every peer places the pieces by itself.
    public sealed class FieldOreBuildSystem : ITickable {
        private readonly IFieldOres _fieldOres;
        private readonly IFieldOreMotion _fieldOreMotion;
        private readonly IFieldOreLifetimeRule _fieldOreLifetimeRule;
        private readonly IFieldOreClock _fieldOreClock;
        private readonly IFieldOreSceneBuilder _fieldOreSceneBuilder;
        private readonly IWeaponHolderAnchors _weaponHolderAnchors;
        private readonly FieldOreSceneModel _fieldOreSceneModel;

        public FieldOreBuildSystem(IFieldOres fieldOres, IFieldOreMotion fieldOreMotion, IFieldOreLifetimeRule fieldOreLifetimeRule, IFieldOreClock fieldOreClock,
                                   IFieldOreSceneBuilder fieldOreSceneBuilder, IWeaponHolderAnchors weaponHolderAnchors, FieldOreSceneModel fieldOreSceneModel) {
            _fieldOres = fieldOres;
            _fieldOreMotion = fieldOreMotion;
            _fieldOreLifetimeRule = fieldOreLifetimeRule;
            _fieldOreClock = fieldOreClock;
            _fieldOreSceneBuilder = fieldOreSceneBuilder;
            _weaponHolderAnchors = weaponHolderAnchors;
            _fieldOreSceneModel = fieldOreSceneModel;
        }

        public void Tick() {
            ReleaseGone();
            if (!_fieldOres.HasField)
                return;

            if (!_fieldOreSceneModel.HasRoot)
                _fieldOreSceneModel.SetRoot(_fieldOreSceneBuilder.CreateRoot());

            double now = _fieldOreClock.Now;
            for (int i = 0; i < _fieldOres.Count; i++)
                Show(_fieldOres.Get(i), now);
        }

        private void Show(in FieldOreEntry entry, double now) {
            if (!_fieldOreSceneModel.TryGetView(entry.Id, out FieldOreView view)) {
                view = _fieldOreSceneBuilder.Build(_fieldOreSceneModel.Root, entry);
                _fieldOreSceneModel.Add(entry.Id, view);
            }

            view.Place(_fieldOreMotion.GetWorldPosition(entry, now), _fieldOreLifetimeRule.GetVisibleShare(entry, now));
            if (!string.IsNullOrEmpty(entry.HookerId) && _weaponHolderAnchors.TryGetAnchor(new PlayerKey(entry.HookerId), out WeaponHolderAnchor anchor))
                view.ShowRope(anchor.Origin);
            else
                view.HideRope();
        }

        // Backwards: a released piece leaves the id list.
        private void ReleaseGone() {
            for (int i = _fieldOreSceneModel.Ids.Count - 1; i >= 0; i--) {
                int id = _fieldOreSceneModel.Ids[i];
                if (_fieldOres.HasField && _fieldOres.TryFindIndex(id, out _))
                    continue;

                _fieldOreSceneModel.TryGetView(id, out FieldOreView view);
                _fieldOreSceneBuilder.Release(view);
                _fieldOreSceneModel.Remove(id);
            }
        }
    }
}
