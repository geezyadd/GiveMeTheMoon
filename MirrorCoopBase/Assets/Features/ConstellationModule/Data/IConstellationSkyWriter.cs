using Features.ConstellationModule.Data.Generated;

namespace Features.ConstellationModule.Data {
    // The server's side of the lobby sky's synced model: the state it holds now and the one write that changes it.
    public interface IConstellationSkyWriter {
        public ConstellationSkyState State { get; }

        public void ServerSetState(ConstellationSkyState value);
    }
}
