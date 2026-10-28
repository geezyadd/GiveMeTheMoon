using System.Collections.Generic;
using Features.BugModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Network;
using Features.FieldAgentModule.Scripts.Network.Generated;
using Features.ShipModule.Scripts;
using Features.SonarModule.Contracts;
using UnityEngine;

namespace Features.BugModule.Scripts.Sonar {
    // Every bug of the last agent batch this peer got, one contact each of the bug's own sonar kind; a bug on the
    // ship is placed on this peer's ship.
    public sealed class BugSonarContactSource : ISonarContactSource {
        private const string BUG_ID_PREFIX = "bug:";
        private const string BUG_ID_FORMAT = BUG_ID_PREFIX + "{0}";

        private readonly IReadOnlyFieldAgentsModel _fieldAgentsModel;
        private readonly IShipClientBindingModel _shipClientBindingModel;
        private readonly IBugKindService _bugKindService;
        private readonly BugConfiguration _bugConfiguration;

        public BugSonarContactSource(
            IReadOnlyFieldAgentsModel fieldAgentsModel,
            IShipClientBindingModel shipClientBindingModel,
            IBugKindService bugKindService,
            BugConfiguration bugConfiguration) {
            _fieldAgentsModel = fieldAgentsModel;
            _shipClientBindingModel = shipClientBindingModel;
            _bugKindService = bugKindService;
            _bugConfiguration = bugConfiguration;
        }

        public void CollectContacts(Vector3 center, float radius, List<SonarContact> results) {
            FieldAgentSnapshot[] agents = _fieldAgentsModel.Frame.Agents;
            if (agents == null)
                return;

            foreach (FieldAgentSnapshot agent in agents) {
                if (TryGetWorldPosition(agent, out Vector3 position) && Vector3.Distance(position, center) <= radius)
                    results.Add(CreateContact(agent.Id, position));
            }
        }

        public bool TryGetContact(string contactId, out SonarContact contact) {
            contact = default;
            FieldAgentSnapshot[] agents = _fieldAgentsModel.Frame.Agents;
            if (agents == null || TryParseId(contactId, out int id) == false)
                return false;

            foreach (FieldAgentSnapshot agent in agents) {
                if (agent.Id != id || TryGetWorldPosition(agent, out Vector3 position) == false)
                    continue;

                contact = CreateContact(id, position);
                return true;
            }

            return false;
        }

        private bool TryGetWorldPosition(in FieldAgentSnapshot agent, out Vector3 position) {
            position = agent.Position;
            if (_bugKindService.IsBug(agent.KindIndex) == false)
                return false;

            if ((FieldAgentMode)agent.Mode == FieldAgentMode.Flight)
                return true;

            ShipBase ship = _shipClientBindingModel.Ship;
            if (ship == null)
                return false;

            position = ship.transform.TransformPoint(agent.Position);
            return true;
        }

        private SonarContact CreateContact(int id, Vector3 position) =>
            new(string.Format(BUG_ID_FORMAT, id), _bugConfiguration.SonarKind, position, _bugConfiguration.Profile.Radius);

        private static bool TryParseId(string contactId, out int id) {
            id = 0;
            return contactId.StartsWith(BUG_ID_PREFIX) && int.TryParse(contactId.Substring(BUG_ID_PREFIX.Length), out id);
        }
    }
}
