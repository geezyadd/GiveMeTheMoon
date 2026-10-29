namespace Features.ParcelModule.Data {
    // The server's writes to the synced parcel model: the station's offers and the parcels the crew took.
    public interface IParcelsWriter {
        public void ServerAddOffers(ParcelOffer value);
        public void ServerSetOffers(int index, ParcelOffer value);
        public void ServerClearOffers();
        public void ServerAddEntries(ParcelEntry value);
        public void ServerSetEntries(int index, ParcelEntry value);
        public void ServerRemoveEntriesAt(int index);
    }
}
