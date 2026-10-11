using Features.GrabModule.Scripts;
using Mirror;
using UnityEngine;

namespace Features.ShopModule.Scripts.Network {
    // The station's shop counter. It is part of the landing pad, so it appears, moves and disappears with the pad;
    // the player's ShopCustomer decides whether this player may open the shop.
    public sealed class ShopKioskInteractable : InteractableBase {
        private const string MISSING_CUSTOMER_ERROR = "{0} has no " + nameof(ShopCustomer) + ".";

        [SerializeField] private Transform _deliveryPoint;

        private NetworkIdentity _lastUser;
        private ShopCustomer _lastCustomer;

        public Transform DeliveryPoint =>
            _deliveryPoint;

        public override bool CanUse(NetworkIdentity user, GrabController grab) =>
            grab.IsHandFree && FindCustomer(user).CanOpenShopAt(transform);

        public override void ServerUse(NetworkIdentity user, GrabController grab) =>
            FindCustomer(user).ServerOpenShop(this);

        // The aim asks every frame while the kiosk is under the crosshair: the lookup runs once per user.
        private ShopCustomer FindCustomer(NetworkIdentity user) {
            if (user == _lastUser)
                return _lastCustomer;

            if (user.TryGetComponent(out ShopCustomer customer) == false)
                throw new MissingComponentException(string.Format(MISSING_CUSTOMER_ERROR, user.name));

            _lastUser = user;
            _lastCustomer = customer;
            return customer;
        }
    }
}
