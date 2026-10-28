using Features.FlightFieldModule.Scripts.Ore;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    public sealed class FixedOreClock : IFieldOreClock {
        public double Now { get; set; }
    }
}
