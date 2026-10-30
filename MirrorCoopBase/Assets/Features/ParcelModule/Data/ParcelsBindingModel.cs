namespace Features.ParcelModule.Data {
    // The synced parcel model's writer on the server, while its bridge on the ship is spawned.
    public sealed class ParcelsBindingModel {
        public IParcelsWriter Writer { get; private set; }
        public bool IsBound => Writer != null;

        public void Bind(IParcelsWriter writer) =>
            Writer = writer;

        public void Clear(IParcelsWriter writer) {
            if (Writer == writer)
                Writer = null;
        }
    }
}
