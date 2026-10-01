using Features.AddressablesConstantsGenerator.Generated;
using Features.CharacterMovableModule.Scripts.PlayerStats;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Features.Zenject.Zenject.Addons.AddressablesConfigurationsLoader;
using Game.Connection;
using Zenject;

namespace Features.GameCoreModule.Scripts.Installers {
    public sealed class ConfigurationInstaller : Installer<ConfigurationInstaller> {
        public override void InstallBindings() {
            Container.BindConfigurationFromAddressables<ConnectionConfig>(Address.Configurations.ConnectionConfig_Default)
                .AsSingle();
            Container.BindConfigurationFromAddressables<ShipRunConfig>(Address.Configurations.ShipRunConfig_Default)
                .AsSingle();
            Container.BindConfigurationFromAddressables<ShipFlightSettings>(Address.Configurations.ShipFlightConfig_Default)
                .AsSingle();
            Container.BindConfigurationFromAddressables<ShipAccumulativeStatsConfiguration>(
                    Address.Configurations.ShipAccumulativeStatsConfiguration_Default)
                .AsSingle();
            Container.BindConfigurationFromAddressables<ShopCatalog>(ShopConfigurationAddresses.SHOP_CATALOG_DEFAULT).AsSingle();
            Container.BindConfigurationFromAddressables<WalletConfiguration>(ShopConfigurationAddresses.WALLET_CONFIGURATION_DEFAULT).AsSingle();
            Container.BindConfigurationFromAddressables<PlayerStatsConfiguration>(PlayerConfigurationAddresses.PLAYER_STATS_DEFAULT).AsSingle();
        }
    }
}
