using Features.FieldAgentModule.Scripts.Network.Generated;
using Features.ShipModule.Scripts.Contracts;
using Zenject;

namespace Features.FieldAgentModule.Scripts.Network {
    // The FieldAgents bridge as a component on the ship's network identity (a generated bridge file cannot be added to
    // a GameObject itself); the server finds it through the registry to send the batches.
    public sealed class FieldAgentSync : FieldAgentsBridge {
        private IComponentRegistry<FieldAgentSync> _fieldAgentSyncRegistry;

        [Inject]
        private void InjectDependencies(IComponentRegistry<FieldAgentSync> fieldAgentSyncRegistry) =>
            _fieldAgentSyncRegistry = fieldAgentSyncRegistry;

        private void OnEnable() =>
            _fieldAgentSyncRegistry.Add(this);

        private void OnDisable() =>
            _fieldAgentSyncRegistry.Remove(this);
    }
}
