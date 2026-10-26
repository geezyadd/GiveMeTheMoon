using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Structures;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // The one place a built rock gets its weapon target id and its hit feedback; the scene builders call it for
    // cluster and debris rocks alike.
    public sealed class FieldRockTargetBinder : IFieldRockTargetBinder {
        private readonly IFieldRockTargetIds _fieldRockTargetIds;
        private readonly IFieldRockHitFeedback _fieldRockHitFeedback;

        public FieldRockTargetBinder(IFieldRockTargetIds fieldRockTargetIds, IFieldRockHitFeedback fieldRockHitFeedback) {
            _fieldRockTargetIds = fieldRockTargetIds;
            _fieldRockHitFeedback = fieldRockHitFeedback;
        }

        public void Bind(FieldRockVisual rock, FieldRockId id) =>
            rock.Target.Bind(_fieldRockTargetIds.GetTargetId(id), _fieldRockHitFeedback);
    }
}
