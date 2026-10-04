using Features.AddressablesConstantsGenerator.Generated;
using Features.CharacterMovableModule.Scripts.PlayerStats;
using Features.GrabModule.Scripts;
using Features.PlayerLifeModule.Scripts;
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
            Container.BindConfigurationFromAddressables<ShopCatalog>(Address.Configurations.ShopCatalog_Default).AsSingle();
            Container.BindConfigurationFromAddressables<WalletConfiguration>(Address.Configurations.WalletConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<PlayerStatsConfiguration>(Address.Configurations.PlayerStatsConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<PlayerDamageConfiguration>(Address.Configurations.PlayerDamageConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<PlayerLifeConfiguration>(Address.Configurations.PlayerLifeConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<GrabConfiguration>(Address.Configurations.GrabConfiguration_Default).AsSingle();
        }
    }
}
