using Features.SavingSystemModule.Contracts;
using Zenject;

namespace Features.SavingSystemModule {
    // Needs SavingSystemConfiguration bound (ConfigurationInstaller). Called from GameCore's ProjectContextInstaller.
    public sealed class SavingSystemModuleInstaller : Installer<SavingSystemModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<ISaveFolderPath>().To<SteamUserSaveFolderPath>().AsSingle();
            Container.Bind<SaveFileCipher>()
                .FromMethod(context => new SaveFileCipher(context.Container.Resolve<SavingSystemConfiguration>().EncryptionPassphrase))
                .AsSingle();
            Container.Bind<SaveFileStorage>().AsSingle();
            Container.Bind<SaveDataStore>().AsSingle();
            Container.Bind<ISaveService>().To<SaveService>().AsSingle();
        }
    }
}
