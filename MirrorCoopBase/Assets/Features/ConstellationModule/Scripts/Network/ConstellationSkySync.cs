using Features.ConstellationModule.Data;
using Features.ConstellationModule.Data.Generated;
using Zenject;

namespace Features.ConstellationModule.Scripts.Network {
    // Stands in LobbyScene: carries the server's lobby sky (ConstellationSky model) to every client and late joiner and
    // is the sky's writer while the server runs.
    public sealed class ConstellationSkySync : ConstellationSkyBridge, IConstellationSkyWriter {
        private ConstellationSkyBindingModel _constellationSkyBindingModel;

        [Inject]
        private void InjectDependencies(ConstellationSkyBindingModel constellationSkyBindingModel) =>
            _constellationSkyBindingModel = constellationSkyBindingModel;

        public override void OnStartServer() {
            base.OnStartServer();
            _constellationSkyBindingModel.Bind(this);
        }

        public override void OnStopServer() =>
            _constellationSkyBindingModel.Clear(this);
    }
}
