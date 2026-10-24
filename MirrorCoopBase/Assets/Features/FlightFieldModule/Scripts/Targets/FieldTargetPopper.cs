using System;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Server;
using Features.NetworkModelModule.Scripts;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Server: the one entry that pops a target balloon. A balloon of the current field that exists and is not popped
    // yet goes into the synced popped list (every peer hides it and plays its pop) and TargetPopped fires, so it fires
    // once per balloon whoever asks and however often. The list is emptied with the field.
    public sealed class FieldTargetPopper : IFieldTargetPopper, IFieldTargetEvents, IFieldTargetReset {
        private readonly IReadOnlyFlightFieldServerModel _flightFieldServerModel;
        private readonly IFieldTargetQuery _fieldTargetQuery;
        private readonly IFieldPoppedTargets _fieldPoppedTargets;
        private readonly IFieldPoppedTargetWriter _fieldPoppedTargetWriter;

        public event Action<FieldTargetPopped> TargetPopped;

        public FieldTargetPopper(IReadOnlyFlightFieldServerModel flightFieldServerModel, IFieldTargetQuery fieldTargetQuery, IFieldPoppedTargets fieldPoppedTargets,
                                 IFieldPoppedTargetWriter fieldPoppedTargetWriter) {
            _flightFieldServerModel = flightFieldServerModel;
            _fieldTargetQuery = fieldTargetQuery;
            _fieldPoppedTargets = fieldPoppedTargets;
            _fieldPoppedTargetWriter = fieldPoppedTargetWriter;
        }

        public FieldTargetPopResult ServerPop(FieldTargetId id, FieldTargetPopCause cause, PlayerKey shooter, string weaponId = "") {
            if (!_fieldPoppedTargetWriter.IsAvailable || !_flightFieldServerModel.HasField)
                return FieldTargetPopResult.NoField;

            if (_fieldPoppedTargets.IsPopped(id))
                return FieldTargetPopResult.AlreadyPopped;

            if (!_fieldTargetQuery.TryGetTarget(id, out FieldTargetDescription target))
                return FieldTargetPopResult.NotFound;

            _fieldPoppedTargetWriter.ServerAdd(id);
            TargetPopped?.Invoke(new FieldTargetPopped(target, cause, shooter, weaponId, _flightFieldServerModel.Number, _flightFieldServerModel.Field.LoopIndex));
            return FieldTargetPopResult.Popped;
        }

        public void ServerReset() =>
            _fieldPoppedTargetWriter.ServerClear();
    }
}
