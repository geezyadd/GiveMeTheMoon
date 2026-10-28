namespace Features.FlightFieldModule.Scripts.Ore {
    // Server: one look at every rope of the field. A piece pulled all the way in reaches the ship; a rope whose holder
    // no longer holds the weapon that threw it, or stands farther from the piece than the rope is long, breaks.
    public interface IFieldOreRopeService {
        public void ServerUpdateRopes();
    }
}
