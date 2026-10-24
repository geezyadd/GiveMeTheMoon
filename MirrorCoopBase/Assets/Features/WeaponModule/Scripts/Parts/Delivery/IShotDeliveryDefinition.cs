namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // The serialized settings of a shot delivery inside a weapon definition.
    public interface IShotDeliveryDefinition {
        // How many target hits one shot may report; the server's fire rate budget is counted in hits.
        public int MaxHitsPerShot { get; }

        // Every holder gets its own runtime delivery, so the shared asset is never changed at runtime.
        public IShotDelivery CreateDelivery(IShotDeliveryContext context);
    }
}
