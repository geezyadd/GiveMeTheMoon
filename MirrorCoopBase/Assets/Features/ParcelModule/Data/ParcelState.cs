namespace Features.ParcelModule.Data {
    // Where a parcel is: a plain crate (on the deck, on a pad or in a hand), under a bug that steals it, drifting in
    // the field after its thief was shot, or on a harpoon rope on its way back to the ship.
    public enum ParcelState {
        None = 0,
        Loose = 1,
        Carried = 2,
        Drifting = 3,
        Pulled = 4
    }
}
