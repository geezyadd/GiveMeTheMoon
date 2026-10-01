#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    public sealed class LoopSmokeErrorLog : IDisposable {
        // Steamworks is not initialised in the editor without a running Steam client; direct hosting does not need it.
        private static readonly string[] _allowedMessageParts = {
            "SteamAPI_Init() failed"
        };

        private readonly List<string> _errors = new List<string>();
        private readonly object _lock = new object();

        public int Count {
            get {
                lock (_lock) {
                    return _errors.Count;
                }
            }
        }

        public LoopSmokeErrorLog() =>
            Application.logMessageReceivedThreaded += OnLogMessageReceived;

        public void Dispose() =>
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;

        public string Describe() {
            lock (_lock) {
                return string.Join("\n---\n", _errors);
            }
        }

        private void OnLogMessageReceived(string message, string stackTrace, LogType type) {
            if (IsFailure(type) == false || IsAllowed(message))
                return;

            lock (_lock) {
                _errors.Add($"[{type}] {message}\n{stackTrace}");
            }
        }

        private static bool IsFailure(LogType type) =>
            type == LogType.Error || type == LogType.Exception || type == LogType.Assert;

        private static bool IsAllowed(string message) {
            for (int i = 0; i < _allowedMessageParts.Length; i++) {
                if (message.Contains(_allowedMessageParts[i]))
                    return true;
            }

            return false;
        }
    }
}
#endif
