using Features.ParcelModule.Data;
using Features.ParcelModule.Data.Generated;
using Zenject;

namespace Features.ParcelModule.Scripts.Network {
    // The Parcels bridge as a component on the ship's network identity (a generated bridge file cannot be added to a
    // GameObject itself); the server writes the offers and the parcels through it while it is spawned.
    public sealed class ParcelSync : ParcelsBridge, IParcelsWriter {
        private ParcelsBindingModel _parcelsBindingModel;

        [Inject]
        private void InjectDependencies(ParcelsBindingModel parcelsBindingModel) =>
            _parcelsBindingModel = parcelsBindingModel;

        public override void OnStartServer() {
            base.OnStartServer();
            _parcelsBindingModel.Bind(this);
        }

        public override void OnStopServer() =>
            _parcelsBindingModel.Clear(this);
    }
}
