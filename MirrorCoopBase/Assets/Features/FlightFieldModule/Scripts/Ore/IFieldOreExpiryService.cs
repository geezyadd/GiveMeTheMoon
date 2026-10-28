namespace Features.FlightFieldModule.Scripts.Ore {
    // Server: takes every piece of ore nobody picked up out of the field at its end of life.
    public interface IFieldOreExpiryService {
        public void ServerRemoveExpired();
    }
}
