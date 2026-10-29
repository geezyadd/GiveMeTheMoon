namespace Features.CutsceneModule.Contracts {
    public enum CutsceneShotKind {
        None = 0,
        // The camera moves to its point and looks at a fixed point.
        LookAtPoint = 1,
        // The camera moves to its point and keeps looking at a target every peer finds by its id (a flock's middle).
        FollowTarget = 2,
    }
}
