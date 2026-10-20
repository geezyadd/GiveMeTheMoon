namespace Features.FlightFieldModule.Scripts.Configuration {
    // How a profile builds its field; every generation step says which layouts it serves.
    public enum FieldLayoutKind {
        None = 0,
        // Walls, ridges and winding corridors (Open, Belt, Canyon, Maze, Channels).
        Corridors = 1,
        // One open asteroid field: rocks of every size to fly between, no walls and no corridors.
        Obstacles = 2,
    }
}
