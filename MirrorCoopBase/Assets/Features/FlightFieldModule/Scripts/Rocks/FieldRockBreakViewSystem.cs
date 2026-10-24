using System;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Structures;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Every peer: a rock that joins the live field's broken list is hidden if this peer has it standing, and the
    // break effect plays where it stood. A rock that is not standing here (far away, or the field is not built yet,
    // as for a late joiner's snapshot) has nothing to hide; the builders leave it hidden when they build it later.
    // The effects stand under the debris root: it follows a world shift like the field root, and the field root
    // keeps one child per structure.
    public sealed class FieldRockBreakViewSystem : IInitializable, IDisposable {
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly IReadOnlyFlightFieldSceneModel _flightFieldSceneModel;
        private readonly IFieldStructureSceneBuilder _fieldStructureSceneBuilder;
        private readonly IFieldDebrisSceneBuilder _fieldDebrisSceneBuilder;
        private readonly IFieldRockBreakEffectPool _fieldRockBreakEffectPool;

        public FieldRockBreakViewSystem(IReadOnlyLiveFieldModel liveFieldModel, IReadOnlyFlightFieldSceneModel flightFieldSceneModel,
                                        IFieldStructureSceneBuilder fieldStructureSceneBuilder, IFieldDebrisSceneBuilder fieldDebrisSceneBuilder,
                                        IFieldRockBreakEffectPool fieldRockBreakEffectPool) {
            _liveFieldModel = liveFieldModel;
            _flightFieldSceneModel = flightFieldSceneModel;
            _fieldStructureSceneBuilder = fieldStructureSceneBuilder;
            _fieldDebrisSceneBuilder = fieldDebrisSceneBuilder;
            _fieldRockBreakEffectPool = fieldRockBreakEffectPool;
        }

        public void Initialize() =>
            _liveFieldModel.OnBrokenRocksAdded += OnBrokenRockAdded;

        public void Dispose() =>
            _liveFieldModel.OnBrokenRocksAdded -= OnBrokenRockAdded;

        private bool TryGetBuiltRock(FieldRockId id, out FieldRockVisual rock) =>
            id.Source switch {
                FieldRockSource.Cluster => _fieldStructureSceneBuilder.TryGetBuiltRock(id.PlacementIndex, id.RockIndex, out rock),
                FieldRockSource.Debris => _fieldDebrisSceneBuilder.TryGetBuiltRock(id.DebrisCell, id.RockIndex, out rock),
                _ => throw new ArgumentOutOfRangeException(nameof(id), id.Source, $"Broken rock {id} has no source."),
            };

        private void OnBrokenRockAdded(int index, FieldRockId id) {
            bool isShowingLiveField = _flightFieldSceneModel.HasRoot && _flightFieldSceneModel.BuiltNumber == _liveFieldModel.Number;
            if (!isShowingLiveField || !TryGetBuiltRock(id, out FieldRockVisual rock) || !rock.gameObject.activeSelf)
                return;

            rock.gameObject.SetActive(false);
            _fieldRockBreakEffectPool.Play(_flightFieldSceneModel.DebrisRoot, rock.transform.position, rock.BoundingDiameter, rock.Material);
        }
    }
}
