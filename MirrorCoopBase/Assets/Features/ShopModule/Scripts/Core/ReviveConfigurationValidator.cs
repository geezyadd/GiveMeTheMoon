using System;
using Features.ShopModule.Scripts.Configurations;
using Zenject;

namespace Features.ShopModule.Scripts.Core {
    // Fails the startup when the buy-back price is not positive, instead of handing out free or paid-for revives.
    public sealed class ReviveConfigurationValidator : IInitializable {
        private const string PRICE_ERROR = "{0}: {1} must be above 0, got {2}.";

        private readonly ReviveConfiguration _reviveConfiguration;
        private readonly IReviveRule _reviveRule;

        public ReviveConfigurationValidator(ReviveConfiguration reviveConfiguration, IReviveRule reviveRule) {
            _reviveConfiguration = reviveConfiguration;
            _reviveRule = reviveRule;
        }

        public void Initialize() {
            long price = _reviveConfiguration.RevivePrice;
            if (_reviveRule.IsPriceValid(price) == false)
                throw new InvalidOperationException(string.Format(
                    PRICE_ERROR,
                    _reviveConfiguration.name,
                    nameof(ReviveConfiguration.RevivePrice),
                    price));
        }
    }
}
