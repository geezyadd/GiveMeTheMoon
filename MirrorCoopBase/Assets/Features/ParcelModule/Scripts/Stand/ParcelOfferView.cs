using Features.ParcelModule.Data;
using Features.ParcelModule.Scripts.Services;
using TMPro;
using UnityEngine;
using Zenject;

namespace Features.ParcelModule.Scripts.Stand {
    // Every peer: a stand slot shows its open offer (a crate on the slot and its reward) and nothing once it is taken,
    // when the station offers none or the ship is not standing here; the slot's aim trigger is there only for an open
    // offer, so it never hides the taken parcel's crate from the crosshair. The text changes only when the offer does.
    public sealed class ParcelOfferView : MonoBehaviour {
        private const string OFFER_FORMAT = "PARCEL\n+{0}\nto the next station\n[E] take";
        private const long NO_REWARD = -1;

        [SerializeField] private int _slot;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Renderer[] _crateLook;
        [SerializeField] private Collider _aimTrigger;

        private IParcelStandService _parcelStandService;
        private long _shownReward = NO_REWARD;
        private bool _isShowing = true;

        [Inject]
        private void InjectDependencies(IParcelStandService parcelStandService) =>
            _parcelStandService = parcelStandService;

        private void Update() {
            bool isOpen = TryGetOpenOffer(out long reward);
            if (isOpen == _isShowing && reward == _shownReward)
                return;

            _isShowing = isOpen;
            _shownReward = reward;
            foreach (Renderer part in _crateLook)
                part.enabled = isOpen;

            _label.enabled = isOpen;
            _aimTrigger.enabled = isOpen;
            if (isOpen)
                _label.text = string.Format(OFFER_FORMAT, reward);
        }

        private bool TryGetOpenOffer(out long reward) {
            reward = NO_REWARD;
            if (_parcelStandService.IsOffering(transform.position) == false || _parcelStandService.TryGetOffer(_slot, out ParcelOffer offer) == false || offer.IsTaken)
                return false;

            reward = offer.Reward;
            return true;
        }
    }
}
