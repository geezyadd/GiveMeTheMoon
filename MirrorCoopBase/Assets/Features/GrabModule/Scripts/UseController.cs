using Mirror;
using UnityEngine;
using Zenject;

namespace Features.GrabModule.Scripts {
    // Server side of using an interactable: checks reach and that the use is still allowed, then performs it.
    public sealed class UseController : NetworkBehaviour {
        [SerializeField] private GrabController _grab;

        private InteractionReach _reach;

        [Inject]
        private void Construct(InteractionReach reach) =>
            _reach = reach;

        public bool CanUse(InteractableBase target) =>
            target != null && target.CanUse(netIdentity, _grab);

        public void RequestUse(InteractableBase target) {
            if (CanUse(target))
                CmdUse(target);
        }

        [Command]
        private void CmdUse(InteractableBase target) {
            if (target == null || _reach.Contains(transform, target.transform.position) == false)
                return;

            if (target.CanUse(netIdentity, _grab))
                target.ServerUse(netIdentity, _grab);
        }
    }
}
