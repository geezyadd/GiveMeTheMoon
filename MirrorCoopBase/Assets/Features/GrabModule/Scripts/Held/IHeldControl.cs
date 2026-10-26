using Mirror;

namespace Features.GrabModule.Scripts.Held {
    // Server side of an interactable the player takes by holding the interact button and moves while it holds it (a
    // lever). UseController calls it only after its reach and CanUse checks; the control decides who holds it.
    public interface IHeldControl {
        public bool ServerTryTakeHold(NetworkIdentity user);

        public void ServerLetGo(NetworkIdentity user);

        // The holder's new position of the control, sent by its peer.
        public void ServerReceivePosition(NetworkIdentity user, float position);
    }
}
