namespace Features.GrabModule.Scripts {
    // What the local player's crosshair is on this frame.
    public readonly struct InteractionTarget {
        public InteractionTarget(Grabbable grabbable, InteractableBase usable, InteractableBase lookedAt) {
            Grabbable = grabbable;
            Usable = usable;
            LookedAt = lookedAt;
        }

        // A free item the empty hand can take.
        public Grabbable Grabbable { get; }

        // The first interactable under the crosshair that can be used right now.
        public InteractableBase Usable { get; }

        // The first interactable under the crosshair, usable or not (the tooltip explains why not).
        public InteractableBase LookedAt { get; }
    }
}
