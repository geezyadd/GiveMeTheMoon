using Features.CharacterMovableModule.Scripts.PlayerStats;
using Features.StatsModule.EntityStatsModule.Scripts.StatsEntity.Factories;
using Zenject;

namespace Features.CharacterMovableModule.Scripts {
    public sealed class CharacterMovableModuleInstaller : Installer<CharacterMovableModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesAndSelfTo<CharacterInputBuffer>().AsSingle();
            Container.Bind<IStatFactory<PlayerStatType>>().To<PlayerStatFactory>().AsSingle();
            Container.Bind<IStatEntityFactory<PlayerStatType>>().To<PlayerStatEntityFactory>().AsSingle();
        }
    }
}
