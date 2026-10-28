#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using Tests.Coop.Shared;
using UnityEngine;

namespace Tests.Coop.Client {
    // The host's commands taken one by one in the order it wrote them: a name and its arguments.
    public sealed class CoopClientCommands {
        private readonly CoopTestClient _client;
        private readonly CoopTestExchange _exchange;
        private readonly WaitForSeconds _poll = new WaitForSeconds(CoopTestExchange.POLL_SECONDS);
        private float _nextPollTime;
        private int _taken;

        public string Name { get; private set; }
        public string[] Arguments { get; private set; } = Array.Empty<string>();

        public CoopClientCommands(CoopTestClient client, CoopTestExchange exchange) {
            _client = client;
            _exchange = exchange;
        }

        public IEnumerator WaitForNextCoroutine() =>
            _client.WaitForCoroutine(TryTakeNext, "the host's next command", _poll);

        // Polled every frame by a step that keeps working until the host's next command (firing until "stop-fire").
        public bool TryTakeNextPolled() {
            if (Time.realtimeSinceStartup < _nextPollTime)
                return false;

            _nextPollTime = Time.realtimeSinceStartup + CoopTestExchange.POLL_SECONDS;
            return TryTakeNext();
        }

        private bool TryTakeNext() {
            List<string> commands = _exchange.ReadCommandsFrom(_taken);
            if (commands.Count == 0)
                return false;

            _taken++;
            string[] parts = commands[0].Split(CoopWeaponsEconomySteps.ARGUMENT_SEPARATOR);
            Name = parts[0];
            Arguments = parts.Length > 1 ? parts[1..] : Array.Empty<string>();
            Debug.Log($"[CoopClient] host command: {commands[0]}");
            return true;
        }
    }
}
#endif
