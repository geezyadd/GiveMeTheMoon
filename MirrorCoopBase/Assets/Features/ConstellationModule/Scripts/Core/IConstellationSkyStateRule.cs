using Features.ConstellationModule.Data.Generated;

namespace Features.ConstellationModule.Scripts.Core {
    public interface IConstellationSkyStateRule {
        public bool IsShown(ConstellationSkyState state);
        public ConstellationSkyState CreateShown(ConstellationSkyState current, string constellationId, double time);
        public ConstellationSkyState CreateHidden(ConstellationSkyState current, double time);
    }
}
