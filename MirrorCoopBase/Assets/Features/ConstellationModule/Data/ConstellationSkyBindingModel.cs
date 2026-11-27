namespace Features.ConstellationModule.Data {
    // The lobby sky's synced model writer on the server; unbound on a pure client and outside the lobby.
    public sealed class ConstellationSkyBindingModel {
        public IConstellationSkyWriter Writer { get; private set; }
        public bool IsBound => Writer != null;

        public void Bind(IConstellationSkyWriter writer) =>
            Writer = writer;

        public void Clear(IConstellationSkyWriter writer) {
            if (Writer != writer)
                return;

            Writer = null;
        }
    }
}
