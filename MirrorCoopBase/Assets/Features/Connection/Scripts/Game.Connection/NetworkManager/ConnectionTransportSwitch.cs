using System;
using Mirror;
using UnityEngine;

namespace Game.Connection
{
    // Chooses between Telepathy (direct IP) and FizzySteamworks (Steam); only the chosen transport stays enabled.
    internal sealed class ConnectionTransportSwitch
    {
        private readonly Transport _telepathy;
        private readonly Transport _fizzy;

        public ConnectionTransportSwitch(GameObject owner, Transport telepathy, Transport fizzy)
        {
            foreach (Transport candidate in owner.GetComponents<Transport>())
            {
                if (candidate is TelepathyTransport)
                    telepathy = candidate;
                else
                    fizzy = candidate;
            }

            _telepathy = telepathy;
            _fizzy = fizzy;
            EnableOnly(false);
        }

        public bool UsesSteam { get; private set; }

        public Transport Telepathy => _telepathy;

        public Transport Select(bool useSteam)
        {
            Transport selected = useSteam ? _fizzy : _telepathy;
            if (selected == null)
            {
                throw new InvalidOperationException(useSteam
                    ? "FizzySteamworks transport is missing on ConnectionNetwork."
                    : "Telepathy transport is missing on ConnectionNetwork.");
            }

            UsesSteam = useSteam;
            EnableOnly(useSteam);
            return selected;
        }

        void EnableOnly(bool steam)
        {
            if (_telepathy != null)
                _telepathy.enabled = steam == false;
            if (_fizzy != null)
                _fizzy.enabled = steam;
        }
    }
}
