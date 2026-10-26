using Mirror;
using UnityEngine;

namespace Features.GrabModule.Scripts {
    public abstract class InteractableBase : NetworkBehaviour {
        [SerializeField] private Outline _outline;

        public abstract bool CanUse(NetworkIdentity user, GrabController grab);

        // A target used some other way (a held control) keeps this empty default.
        public virtual void ServerUse(NetworkIdentity user, GrabController grab) {
        }

        // A target that is used by holding the interact button: how long the hold lasts. ServerUse runs when it ends.
        public virtual bool TryGetHoldSeconds(GrabController grab, out float seconds) {
            seconds = 0f;
            return false;
        }

        public void SetHovered(bool hovered) {
            if (_outline == null)
                return;

            _outline.enabled = hovered;
        }
    }
}
