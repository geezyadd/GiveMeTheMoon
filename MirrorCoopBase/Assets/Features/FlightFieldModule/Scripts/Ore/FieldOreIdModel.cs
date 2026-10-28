namespace Features.FlightFieldModule.Scripts.Ore {
    // Server only, not synced: the number the next dropped piece of ore gets. It only grows, so an id is never given
    // twice in a session, even across fields.
    public sealed class FieldOreIdModel {
        private int _lastId;

        public int TakeNextId() =>
            ++_lastId;
    }
}
