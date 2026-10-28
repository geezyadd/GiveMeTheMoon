namespace Features.FlightFieldModule.Scripts.Ore {
    public sealed class FieldOreExpiryService : IFieldOreExpiryService {
        private readonly IFieldOres _fieldOres;
        private readonly IFieldOreWriter _fieldOreWriter;
        private readonly IFieldOreLifetimeRule _fieldOreLifetimeRule;
        private readonly IFieldOreClock _fieldOreClock;

        public FieldOreExpiryService(IFieldOres fieldOres, IFieldOreWriter fieldOreWriter, IFieldOreLifetimeRule fieldOreLifetimeRule, IFieldOreClock fieldOreClock) {
            _fieldOres = fieldOres;
            _fieldOreWriter = fieldOreWriter;
            _fieldOreLifetimeRule = fieldOreLifetimeRule;
            _fieldOreClock = fieldOreClock;
        }

        // Backwards: a removed piece moves the ones after it.
        public void ServerRemoveExpired() {
            if (!_fieldOreWriter.IsAvailable || !_fieldOres.HasField)
                return;

            double now = _fieldOreClock.Now;
            for (int i = _fieldOres.Count - 1; i >= 0; i--) {
                if (_fieldOreLifetimeRule.IsExpired(_fieldOres.Get(i), now))
                    _fieldOreWriter.ServerRemoveAt(i);
            }
        }
    }
}
