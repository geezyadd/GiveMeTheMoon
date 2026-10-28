#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Features.FieldAgentModule.Scripts.Client;
using Features.ShipModule.Scripts.Generated;
using Mirror;
using Tests.Shared;
using UnityEngine;

namespace Tests.Coop.Shared {
    // The field agents one peer shows at one network time, as text for the exchange: every agent's shown position from
    // the run's destination (world shifts move both alike), keyed by agent id. Both peers show the agents the same
    // interpolation delay behind the server, so two views taken at the same network time match.
    public sealed class CoopFieldAgentView {
        private const char TIME_SEPARATOR = '|';
        private const char AGENT_SEPARATOR = ';';
        private const char ID_SEPARATOR = ':';
        private const char AXIS_SEPARATOR = ',';
        private const string NUMBER_FORMAT = "R";

        private static readonly List<int> _ids = new();

        public double NetworkTime { get; private set; }
        public Dictionary<int, Vector3> Positions { get; } = new();

        public static CoopFieldAgentView Capture() {
            IFieldAgentShownAgents shown = SessionQueries.Resolve<IFieldAgentShownAgents>();
            Vector3 destination = SessionQueries.Resolve<IReadOnlyShipRunModel>().TransitDestination;
            CoopFieldAgentView view = new() { NetworkTime = Mirror.NetworkTime.time };
            shown.CollectShownIds(_ids);
            foreach (int id in _ids) {
                if (shown.TryGetShownWorldPosition(id, out Vector3 world))
                    view.Positions[id] = world - destination;
            }

            return view;
        }

        public static CoopFieldAgentView Parse(string text) {
            CoopFieldAgentView view = new();
            string[] parts = text.Split(TIME_SEPARATOR);
            view.NetworkTime = double.Parse(parts[0], CultureInfo.InvariantCulture);
            if (parts.Length < 2 || parts[1].Length == 0)
                return view;

            foreach (string agent in parts[1].Split(AGENT_SEPARATOR)) {
                string[] idAndPosition = agent.Split(ID_SEPARATOR);
                string[] axes = idAndPosition[1].Split(AXIS_SEPARATOR);
                view.Positions[int.Parse(idAndPosition[0], CultureInfo.InvariantCulture)] = new Vector3(ParseFloat(axes[0]), ParseFloat(axes[1]), ParseFloat(axes[2]));
            }

            return view;
        }

        public string Format() {
            StringBuilder text = new();
            text.Append(NetworkTime.ToString(NUMBER_FORMAT, CultureInfo.InvariantCulture)).Append(TIME_SEPARATOR);
            bool isFirst = true;
            foreach (KeyValuePair<int, Vector3> agent in Positions) {
                if (isFirst == false)
                    text.Append(AGENT_SEPARATOR);

                isFirst = false;
                text.Append(agent.Key.ToString(CultureInfo.InvariantCulture)).Append(ID_SEPARATOR)
                    .Append(FormatFloat(agent.Value.x)).Append(AXIS_SEPARATOR)
                    .Append(FormatFloat(agent.Value.y)).Append(AXIS_SEPARATOR)
                    .Append(FormatFloat(agent.Value.z));
            }

            return text.ToString();
        }

        private static string FormatFloat(float value) =>
            value.ToString(NUMBER_FORMAT, CultureInfo.InvariantCulture);

        private static float ParseFloat(string text) =>
            float.Parse(text, CultureInfo.InvariantCulture);
    }
}
#endif
