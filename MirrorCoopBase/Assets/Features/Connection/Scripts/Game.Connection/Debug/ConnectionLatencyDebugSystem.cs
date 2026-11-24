#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.DebugModule.Contracts;
using Zenject;

namespace Game.Connection
{
    // Debug only: the debug window's "Lag" button. Each press switches this process to the next network condition from
    // ConnectionConfig. It delays and drops only what this process sends: set it on the client to make that client's
    // movement reach the others late, set it on both for a full round trip. A button label cannot change, so the button
    // registers again with the label of the new condition.
    public sealed class ConnectionLatencyDebugSystem : IInitializable, IDisposable
    {
        const string LABEL_PREFIX = "Lag: ";
        const string DESCRIPTION = "Next network condition for what this game sends (delay, jitter, loss of unreliable messages). Set it on the client whose movement you watch from the host.";
        const string NO_NETWORK_MANAGER = "No network manager yet.";

        readonly ConnectionSessionModel sessionModel;
        readonly IReadOnlyList<ConnectionLatencyProfile> profiles;
        readonly IDebugActionRegistry debugActionRegistry;

        IDisposable registration;
        int activeIndex;

        public ConnectionLatencyDebugSystem(
            ConnectionSessionModel sessionModel,
            ConnectionConfig connectionConfig,
            IDebugActionRegistry debugActionRegistry)
        {
            this.sessionModel = sessionModel;
            profiles = connectionConfig.LatencyProfiles;
            this.debugActionRegistry = debugActionRegistry;
        }

        public void Initialize()
        {
            if (profiles.Count > 0)
                Register();
        }

        public void Dispose() =>
            registration?.Dispose();

        void Register()
        {
            string label = LABEL_PREFIX + profiles[activeIndex].Label;
            registration = debugActionRegistry.Register(
                new DebugActionDefinition(DebugSections.NETWORK, label, DESCRIPTION, ReadUnavailableReason, SwitchToNext));
        }

        string ReadUnavailableReason() =>
            FindSimulation() == null ? NO_NETWORK_MANAGER : null;

        void SwitchToNext()
        {
            ConnectionLatencySimulation simulation = FindSimulation();
            if (simulation == null)
                return;

            activeIndex = (activeIndex + 1) % profiles.Count;
            simulation.Apply(profiles[activeIndex]);
            registration.Dispose();
            Register();
        }

        ConnectionLatencySimulation FindSimulation() =>
            sessionModel.NetworkManager != null ? sessionModel.NetworkManager.LatencySimulation : null;
    }
}
#endif
