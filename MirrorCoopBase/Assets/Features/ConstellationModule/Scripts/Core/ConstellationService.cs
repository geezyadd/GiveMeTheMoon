using System;
using Features.ConstellationModule.Data;
using Features.ConstellationModule.Data.Generated;
using Features.ConstellationModule.Scripts.Definitions;
using Mirror;

namespace Features.ConstellationModule.Scripts.Core {
    // Server: the one place that changes the lobby sky every peer sees. Showing the shown constellation (or all of
    // them) again replays it; hiding an empty sky changes nothing.
    public sealed class ConstellationService : IConstellationService {
        private const string UNKNOWN_CONSTELLATION_FORMAT = "No constellation with id '{0}' in the catalog.";
        private const string NO_WRITER_MESSAGE = "The lobby sky is written on the server only, while the lobby's sky sync is spawned.";

        private readonly ConstellationSkyBindingModel _constellationSkyBindingModel;
        private readonly ConstellationCatalog _constellationCatalog;
        private readonly IConstellationSkyStateRule _constellationSkyStateRule;

        public ConstellationService(ConstellationSkyBindingModel constellationSkyBindingModel, ConstellationCatalog constellationCatalog, IConstellationSkyStateRule constellationSkyStateRule) {
            _constellationSkyBindingModel = constellationSkyBindingModel;
            _constellationCatalog = constellationCatalog;
            _constellationSkyStateRule = constellationSkyStateRule;
        }

        public void ServerShow(string constellationId) {
            if (_constellationCatalog.TryFindIndex(constellationId, out _) == false)
                throw new ArgumentException(string.Format(UNKNOWN_CONSTELLATION_FORMAT, constellationId), nameof(constellationId));

            WriteShown(constellationId);
        }

        public void ServerShowAll() =>
            WriteShown(ConstellationSkyIds.ALL_CONSTELLATIONS);

        public void ServerHide() {
            IConstellationSkyWriter writer = GetWriter();
            ConstellationSkyState current = writer.State;
            if (_constellationSkyStateRule.IsShown(current) == false)
                return;

            writer.ServerSetState(_constellationSkyStateRule.CreateHidden(current, NetworkTime.time));
        }

        private void WriteShown(string constellationId) {
            IConstellationSkyWriter writer = GetWriter();
            writer.ServerSetState(_constellationSkyStateRule.CreateShown(writer.State, constellationId, NetworkTime.time));
        }

        private IConstellationSkyWriter GetWriter() {
            if (_constellationSkyBindingModel.IsBound == false)
                throw new InvalidOperationException(NO_WRITER_MESSAGE);

            return _constellationSkyBindingModel.Writer;
        }
    }
}
