using System;

namespace Features.TargetModule {
    // What a target is for; one target can be several at once.
    [Flags]
    public enum TargetKind {
        None = 0,
        // An attack can go for it and land on it (a player's head, a mob).
        Hittable = 1 << 0,
        // Something may turn to look at it (eyes that notice things).
        LookAt = 1 << 1
    }
}
