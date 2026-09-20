using Mirror;
using UnityEngine;

namespace Features.GrabModule.Scripts {
    public abstract class InteractableBase : NetworkBehaviour {
        [SerializeField] private Outline _outline;

        public abstract bool CanUse(NetworkIdentity user, GrabController grab);

        public abstract void ServerUse(NetworkIdentity user, GrabController grab);

        public void SetHovered(bool hovered) {
            if (_outline == null)
                return;

            _outline.enabled = hovered;
        }
    }
}
