using Features.GrabModule.Scripts;
using Features.ParcelModule.Data;
using Features.ParcelModule.Scripts.Services;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ParcelModule.Scripts.Stand {
    // One slot of the station's delivery stand: E with a free hand takes its parcel for free while the ship stands at
    // this station; the crate appears on the slot's point. Part of the landing pad, so it comes and goes with it.
    public sealed class ParcelOfferInteractable : InteractableBase {
        [SerializeField] private int _slot;
        [SerializeField] private Transform _cratePoint;

        private IParcelStandService _parcelStandService;
        private IParcelOfferService _parcelOfferService;

        [Inject]
        private void InjectDependencies(IParcelStandService parcelStandService, IParcelOfferService parcelOfferService) {
            _parcelStandService = parcelStandService;
            _parcelOfferService = parcelOfferService;
        }

        public override bool CanUse(NetworkIdentity user, GrabController grab) =>
            grab.IsHandFree && IsOfferOpen();

        public override void ServerUse(NetworkIdentity user, GrabController grab) =>
            _parcelOfferService.ServerTryTakeOffer(_slot, _cratePoint.position, _cratePoint.rotation);

        private bool IsOfferOpen() =>
            _parcelStandService.IsOffering(transform.position)
            && _parcelStandService.TryGetOffer(_slot, out ParcelOffer offer)
            && offer.IsTaken == false;
    }
}
