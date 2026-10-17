namespace Features.FlightFieldModule.Scripts.Model {
    // What the generator decided a cell is: closed by giant rocks (the border wall or a ridge), passable with small
    // debris, or empty. The kind of obstacle that stands there comes from the profile's catalog, not from this value.
    public enum FieldCellClass {
        None = 0,
        Wall = 1,
        Ridge = 2,
        Debris = 3,
        Empty = 4,
    }
}
