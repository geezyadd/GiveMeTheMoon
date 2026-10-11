namespace Features.GrabModule.Scripts {
    // Outlines what the interact button would act on and tells the tooltip what is under the crosshair.
    public sealed class InteractionHighlight {
        private readonly HoveredInteractableModel _hoveredModel;

        private InteractableBase _outlinedInteractable;
        private Grabbable _outlinedGrabbable;

        public InteractionHighlight(HoveredInteractableModel hoveredModel) =>
            _hoveredModel = hoveredModel;

        public void Show(InteractionTarget target) {
            InteractableBase interactable = target.Usable;
            Grabbable grabbable = interactable == null ? target.Grabbable : null;
            OutlineInteractable(interactable);
            OutlineGrabbable(grabbable);

            InteractableBase shown = target.Usable != null ? target.Usable : target.LookedAt;
            _hoveredModel.SetInteractable(shown, target.Usable != null);
            _hoveredModel.SetGrabbable(target.Grabbable);
        }

        public void Clear() {
            OutlineInteractable(null);
            OutlineGrabbable(null);
            _hoveredModel.Clear();
        }

        private void OutlineInteractable(InteractableBase next) {
            if (_outlinedInteractable == next)
                return;

            if (_outlinedInteractable != null)
                _outlinedInteractable.SetHovered(false);

            _outlinedInteractable = next;
            if (_outlinedInteractable != null)
                _outlinedInteractable.SetHovered(true);
        }

        private void OutlineGrabbable(Grabbable next) {
            if (_outlinedGrabbable == next)
                return;

            if (_outlinedGrabbable != null)
                _outlinedGrabbable.SetHovered(false);

            _outlinedGrabbable = next;
            if (_outlinedGrabbable != null)
                _outlinedGrabbable.SetHovered(true);
        }
    }
}
