using Features.AddressablesConstantsGenerator.Generated;
using Features.CameraModule.Scripts;
using Features.CharacterMovableModule.Scripts.PlayerStats;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Structures;
using Features.GrabModule.Scripts;
using Features.LobbyModule.Scripts;
using Features.PlayerLifeModule.Scripts;
using Features.PlayerProfileModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Damage;
using Features.ShipModule.Scripts.Hull;
using Features.ShipModule.Scripts.Impacts;
using Features.ShopModule.Scripts.Configurations;
using Features.StagePresetModule.Scripts.Configurations;
using Features.WeaponModule.Scripts.Definitions;
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
            Container.BindConfigurationFromAddressables<ShipHullConfiguration>(Address.Configurations.ShipHullConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<ShipDamageConfiguration>(Address.Configurations.ShipDamageConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<ShipHullHudConfiguration>(Address.Configurations.ShipHullHudConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<ShipRockImpactConfiguration>(Address.Configurations.ShipRockImpactConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<ShopCatalog>(Address.Configurations.ShopCatalog_Default).AsSingle();
            Container.BindConfigurationFromAddressables<WalletConfiguration>(Address.Configurations.WalletConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<ShopKioskConfiguration>(Address.Configurations.ShopKioskConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<ReviveConfiguration>(Address.Configurations.ReviveConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<PlayerStatsConfiguration>(Address.Configurations.PlayerStatsConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<PlayerDamageConfiguration>(Address.Configurations.PlayerDamageConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<PlayerLifeConfiguration>(Address.Configurations.PlayerLifeConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<GrabConfiguration>(Address.Configurations.GrabConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<PlayerNameplateConfiguration>(Address.Configurations.PlayerNameplateConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<ModuleCatalog>(Address.Configurations.ModuleCatalog_Default).AsSingle();
            Container.BindConfigurationFromAddressables<ShipStationCatalog>(Address.Configurations.ShipStationCatalog_Default).AsSingle();
            Container.BindConfigurationFromAddressables<ShipRadarCatalog>(Address.Configurations.ShipRadarCatalog_Default).AsSingle();
            Container.BindConfigurationFromAddressables<CameraCatalog>(Address.Configurations.CameraCatalog_Default).AsSingle();
            Container.BindConfigurationFromAddressables<FlightFieldConfiguration>(Address.Configurations.FlightFieldConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<FieldStructureCatalog>(Address.Configurations.FieldStructureCatalog_Default).AsSingle();
            Container.BindConfigurationFromAddressables<FlightFieldDecorConfiguration>(Address.Configurations.FlightFieldDecorConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<FieldRockDurabilityConfiguration>(Address.Configurations.FieldRockDurabilityConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<FieldTargetCatalog>(Address.Configurations.FieldTargetCatalog_Default).AsSingle();
            Container.BindConfigurationFromAddressables<FieldTargetConfiguration>(Address.Configurations.FieldTargetConfiguration_Default).AsSingle();
            Container.BindConfigurationFromAddressables<StagePresetCatalog>(Address.Configurations.StagePresetCatalog_Default).AsSingle();
            Container.BindConfigurationFromAddressables<WeaponCatalog>(Address.Configurations.WeaponCatalog_Default).AsSingle();
            Container.BindConfigurationFromAddressables<WeaponHitRuleConfiguration>(Address.Configurations.WeaponHitRuleConfiguration_Default).AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Container.BindConfigurationFromAddressables<Features.ShipModule.Scripts.Debug.WeaponDebugTargetConfiguration>(Address.Configurations.WeaponDebugTargetConfiguration_Default).AsSingle();
#endif
        }
    }
}
