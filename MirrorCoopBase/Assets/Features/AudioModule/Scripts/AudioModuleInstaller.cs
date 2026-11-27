using Zenject;

namespace Features.AudioModule {
    public sealed class AudioModuleInstaller : Installer<AudioModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<IAudioService>().To<FmodAudioService>().AsSingle();
            Container.Bind<MusicDuckSources>().AsSingle();
        }
    }
}
