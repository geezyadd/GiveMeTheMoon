using System;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Server;
using Features.NetworkModelModule.Scripts;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Server: the one entry that breaks a rock. A rock of the current field that exists, is breakable and is not
    // broken yet goes into the synced broken list (every peer hides it) and RockDestroyed fires, so it fires once per
    // rock whoever asks and however often. The list is emptied with the field.
    public sealed class FieldRockBreaker : IFieldRockBreaker, IFieldRockEvents, IFieldRockReset {
        private readonly IReadOnlyFlightFieldServerModel _flightFieldServerModel;
        private readonly IFieldRockQuery _fieldRockQuery;
        private readonly IFieldBrokenRocks _fieldBrokenRocks;
        private readonly IFieldBrokenRockWriter _fieldBrokenRockWriter;

        public event Action<FieldRockDestroyed> RockDestroyed;

        public FieldRockBreaker(IReadOnlyFlightFieldServerModel flightFieldServerModel, IFieldRockQuery fieldRockQuery, IFieldBrokenRocks fieldBrokenRocks,
                                IFieldBrokenRockWriter fieldBrokenRockWriter) {
            _flightFieldServerModel = flightFieldServerModel;
            _fieldRockQuery = fieldRockQuery;
            _fieldBrokenRocks = fieldBrokenRocks;
            _fieldBrokenRockWriter = fieldBrokenRockWriter;
        }

        public FieldRockBreakResult ServerBreak(FieldRockId id, FieldRockDestroyCause cause, PlayerKey instigator, string weaponId = "") {
            if (!_fieldBrokenRockWriter.IsAvailable || !_flightFieldServerModel.HasField)
                return FieldRockBreakResult.NoField;

            if (_fieldBrokenRocks.IsBroken(id))
                return FieldRockBreakResult.AlreadyBroken;

            if (!_fieldRockQuery.TryGetRock(id, out FieldRockDescription rock))
                return FieldRockBreakResult.NotFound;

            if (!rock.IsBreakable)
                return FieldRockBreakResult.Unbreakable;

            _fieldBrokenRockWriter.ServerAdd(id);
            RockDestroyed?.Invoke(new FieldRockDestroyed(rock, cause, instigator, weaponId, _flightFieldServerModel.Number, _flightFieldServerModel.Field.LoopIndex));
            return FieldRockBreakResult.Broken;
        }

        public void ServerReset() =>
            _fieldBrokenRockWriter.ServerClear();
    }
}
