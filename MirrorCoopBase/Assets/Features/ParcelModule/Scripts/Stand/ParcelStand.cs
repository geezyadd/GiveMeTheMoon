using Mirror;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Stand {
    // The station's delivery stand, a network object the server puts beside the pad the ship stands at; its offer
    // slots are its children.
    public sealed class ParcelStand : MonoBehaviour {
        [SerializeField] private NetworkIdentity _identity;

        public NetworkIdentity Identity => _identity;
    }
}
