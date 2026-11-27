using Features.ConstellationModule.Scripts.Definitions;

namespace Features.ConstellationModule.Scripts.Core {
    public interface IConstellationRevealTimelineFactory {
        public ConstellationRevealTimeline Create(ConstellationDefinition constellation);
    }
}
