#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Features.PlayerReplicaModule.Receiving;
using Features.DebugModule.Contracts;
using UnityEngine;
using Zenject;

namespace Features.PlayerReplicaModule.Diagnostics {
    // Debug only: the debug window's "Replicas: stats" button. It writes to the console, for every other player's body
    // on this peer, what arrived since the last press: packets a second, packets dropped as old, how old they were, and
    // how often its simulated copy had to snap.
    public sealed class ReplicaStatsDebugSystem : IInitializable, IDisposable {
        private const string LABEL = "Replicas: stats";
        private const string DESCRIPTION = "Writes to the console, for every other player, the state packets that reached this game since the last press: how many a second, how many were dropped as old or out of order, and their age.";
        private const string NO_OTHER_PLAYERS = "No other players' state packets yet.";
        private const double MILLISECONDS_PER_SECOND = 1000d;

        private readonly BodyStateChannelRegistry _registry;
        private readonly IDebugActionRegistry _debugActionRegistry;

        private IDisposable _registration;

        public ReplicaStatsDebugSystem(BodyStateChannelRegistry registry, IDebugActionRegistry debugActionRegistry) {
            _registry = registry;
            _debugActionRegistry = debugActionRegistry;
        }

        public void Initialize() =>
            _registration = _debugActionRegistry.Register(
                new DebugActionDefinition(DebugSections.NETWORK, LABEL, DESCRIPTION, ReadUnavailableReason, WriteReport));

        public void Dispose() =>
            _registration?.Dispose();

        private string ReadUnavailableReason() {
            for (int i = 0; i < _registry.Channels.Count; i++) {
                if (_registry.Channels[i].HasLatest)
                    return null;
            }

            return NO_OTHER_PLAYERS;
        }

        private void WriteReport() {
            double now = Time.unscaledTimeAsDouble;
            for (int i = 0; i < _registry.Channels.Count; i++) {
                BodyStateChannel channel = _registry.Channels[i];
                if (channel.HasLatest == false)
                    continue;

                BodyStateReceiveReport report = channel.ReceiveStats.TakeReport(now);
                UnityEngine.Debug.Log(
                    $"[Replicas] {channel.name} (netId {channel.netId}): {report.PacketsPerSecond:F1} packets/s, " +
                    $"{report.Accepted} taken, {report.Dropped} dropped as old, {report.Snaps} snaps, " +
                    $"age {report.AverageAgeSeconds * MILLISECONDS_PER_SECOND:F0} ms on average, last {report.LastAgeSeconds * MILLISECONDS_PER_SECOND:F0} ms, " +
                    $"error to the owner {report.AverageErrorMeters:F3} m on average, {report.MaxErrorMeters:F3} m at most");
            }
        }
    }
}
#endif
