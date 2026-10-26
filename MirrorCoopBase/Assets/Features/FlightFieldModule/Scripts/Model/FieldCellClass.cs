namespace Features.FlightFieldModule.Scripts.Model {
    // What the generator decided a cell is: closed by a giant rock (Ridge), passable with small debris, or empty.
    // The kind of obstacle that stands there comes from the profile's catalog, not from this value.
    public enum FieldCellClass {
        None = 0,
        Ridge = 2,
        Debris = 3,
        Empty = 4,
    }
}
