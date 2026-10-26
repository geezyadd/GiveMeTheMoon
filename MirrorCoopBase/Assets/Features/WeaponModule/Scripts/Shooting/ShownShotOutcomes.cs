using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.Feel;

namespace Features.WeaponModule.Scripts.Shooting {
    // A peer that replays another player's fire only shows where the shots land; the shooter reports the real hits.
    public sealed class ShownShotOutcomes : IShotOutcomeReceiver {
        private readonly IWeaponFeel _feel;

        public ShownShotOutcomes(IWeaponFeel feel) =>
            _feel = feel;

        public void ReceiveOutcome(in WeaponShotOutcome outcome) =>
            _feel.PresentShot(outcome);
    }
}
