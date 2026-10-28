using Features.FlightFieldModule.Scripts.Network;

namespace Features.FlightFieldModule.Scripts.Ore {
    // How long a piece of ore lasts: a drifting piece is gone at its end of life and fades out before it; a piece on a
    // rope lasts while it is pulled.
    public interface IFieldOreLifetimeRule {
        public bool IsExpired(in FieldOreEntry entry, double now);
        // 1 while it shines fully, down to 0 at its end of life.
        public float GetVisibleShare(in FieldOreEntry entry, double now);
    }
}
