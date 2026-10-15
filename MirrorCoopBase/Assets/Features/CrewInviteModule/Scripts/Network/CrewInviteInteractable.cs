using Features.GrabModule.Scripts;
using Mirror;
using UnityEngine;

namespace Features.CrewInviteModule.Scripts.Network {
    public sealed class CrewInviteInteractable : InteractableBase {
        private const string MISSING_INVITER_ERROR = "{0} has no " + nameof(CrewInviter) + ".";

        private NetworkIdentity _lastUser;
        private CrewInviter _lastInviter;

        public override bool CanUse(NetworkIdentity user, GrabController grab) =>
            FindInviter(user).CanInvite();

        public override void ServerUse(NetworkIdentity user, GrabController grab) =>
            FindInviter(user).ServerOpenInviteOverlay();

        // The aim asks every frame while the terminal is under the crosshair: the lookup runs once per user.
        private CrewInviter FindInviter(NetworkIdentity user) {
            if (user == _lastUser)
                return _lastInviter;

            if (user.TryGetComponent(out CrewInviter inviter) == false)
                throw new MissingComponentException(string.Format(MISSING_INVITER_ERROR, user.name));

            _lastUser = user;
            _lastInviter = inviter;
            return inviter;
        }
    }
}
