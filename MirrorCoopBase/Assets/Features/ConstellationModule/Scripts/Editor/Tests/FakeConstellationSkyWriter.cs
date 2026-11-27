using Features.ConstellationModule.Data;
using Features.ConstellationModule.Data.Generated;

namespace Features.ConstellationModule.Editor.Tests {
    public sealed class FakeConstellationSkyWriter : IConstellationSkyWriter {
        public ConstellationSkyState State { get; private set; }
        public int Writes { get; private set; }

        public void ServerSetState(ConstellationSkyState value) {
            State = value;
            Writes++;
        }
    }
}
