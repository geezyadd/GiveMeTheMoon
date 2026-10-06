using System;
using Features.ShopModule.Scripts.Configurations;
using Zenject;

namespace Features.ShopModule.Scripts.Core {
    // Fails the startup when the kiosk distances are out of order, instead of letting the window flicker or
    // purchases from an open window be refused.
    public sealed class ShopKioskConfigurationValidator : IInitializable {
        private const string ORDER_ERROR = "{0}: expected {1} ({2}) < {3} ({4}) <= {5} ({6}).";

        private readonly ShopKioskConfiguration _shopKioskConfiguration;
        private readonly IShopAccessRule _shopAccessRule;

        public ShopKioskConfigurationValidator(ShopKioskConfiguration shopKioskConfiguration, IShopAccessRule shopAccessRule) {
            _shopKioskConfiguration = shopKioskConfiguration;
            _shopAccessRule = shopAccessRule;
        }

        public void Initialize() {
            float open = _shopKioskConfiguration.OpenDistance;
            float keepOpen = _shopKioskConfiguration.KeepOpenDistance;
            float purchase = _shopKioskConfiguration.PurchaseDistance;
            if (_shopAccessRule.IsDistanceOrderValid(open, keepOpen, purchase) == false)
                throw new InvalidOperationException(string.Format(
                    ORDER_ERROR,
                    _shopKioskConfiguration.name,
                    nameof(ShopKioskConfiguration.OpenDistance),
                    open,
                    nameof(ShopKioskConfiguration.KeepOpenDistance),
                    keepOpen,
                    nameof(ShopKioskConfiguration.PurchaseDistance),
                    purchase));
        }
    }
}
