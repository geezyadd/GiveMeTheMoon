using System;

namespace Features.ShopModule.Scripts.Core {
    // Whether a living player may buy back a dead crewmate: at the kiosk like a purchase, the target online and dead,
    // and the crew can pay.
    public sealed class ReviveRule : IReviveRule {
        public ReviveStatus Evaluate(ReviveRequest request) {
            if (request.BuyerAccess != ShopAccessStatus.Allowed)
                return ToReviveStatus(request.BuyerAccess);

            if (request.IsTargetOnline == false)
                return ReviveStatus.TargetOffline;

            if (request.IsTargetDead == false)
                return ReviveStatus.TargetNotDead;

            if (CanAfford(request.Balance, request.Price) == false)
                return ReviveStatus.NotEnoughMoney;

            return ReviveStatus.Allowed;
        }

        public bool CanAfford(long balance, long price) =>
            balance >= price;

        // A free buy-back would make death meaningless; a negative one would pay the crew.
        public bool IsPriceValid(long price) =>
            price > 0;

        private static ReviveStatus ToReviveStatus(ShopAccessStatus access) =>
            access switch {
                ShopAccessStatus.NotBuildPhase => ReviveStatus.NotBuildPhase,
                ShopAccessStatus.Dead => ReviveStatus.BuyerDead,
                ShopAccessStatus.TooFar => ReviveStatus.TooFar,
                _ => throw new ArgumentOutOfRangeException(nameof(access), access, null)
            };
    }
}
