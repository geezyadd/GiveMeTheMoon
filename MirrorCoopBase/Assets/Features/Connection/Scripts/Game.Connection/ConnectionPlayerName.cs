using Mirror;
using UnityEngine;

namespace Game.Connection
{
    // The authenticator's player name (Steam persona or the direct-IP name) lives in connectionToClient, which only the
    // server has; the player object carries it to every client.
    [DisallowMultipleComponent]
    public class ConnectionPlayerName : NetworkBehaviour
    {
        [SyncVar] string displayName;

        public string DisplayName => displayName;

        public override void OnStartServer()
        {
            if (connectionToClient?.authenticationData is ConnectionAuthenticator.AuthRequestMessage auth)
                displayName = auth.playerName;
        }
    }
}
