using System;
using System.Collections.Generic;

namespace Features.SonarModule.Data {
    // This peer's sonar console: whether the local player stands at it (the server seated them with that key), the
    // last ping, the echoes the waves lit and what the scope shows this frame.
    public sealed class SonarScopeModel {
        private readonly List<SonarEcho> _echoes = new();
        private readonly List<SonarBlip> _blips = new();

        public bool IsOpen { get; private set; }
        public string NavigatorKey { get; private set; } = string.Empty;
        public int OpenedFrame { get; private set; }
        public bool IsSeatConfirmed { get; private set; }
        public float LastPingAt { get; private set; } = float.NegativeInfinity;
        public IReadOnlyList<SonarEcho> Echoes => _echoes;
        public IReadOnlyList<SonarBlip> Blips => _blips;
        public SonarScopeFrame Frame { get; private set; }

        public event Action OnOpenChanged;
        public event Action OnPinged;
        public event Action OnFrameChanged;

        public void Open(string navigatorKey, int frame) {
            if (IsOpen)
                return;

            IsOpen = true;
            NavigatorKey = navigatorKey;
            OpenedFrame = frame;
            OnOpenChanged?.Invoke();
        }

        public void Close() {
            if (IsOpen == false)
                return;

            IsOpen = false;
            IsSeatConfirmed = false;
            NavigatorKey = string.Empty;
            _echoes.Clear();
            _blips.Clear();
            LastPingAt = float.NegativeInfinity;
            OnOpenChanged?.Invoke();
        }

        // The synced model named this peer's navigator key at least once.
        public void ConfirmSeat() =>
            IsSeatConfirmed = true;

        public void StartPing(float now) {
            LastPingAt = now;
            OnPinged?.Invoke();
        }

        public void AddEcho(SonarEcho echo) =>
            _echoes.Add(echo);

        public void ReplaceEchoAt(int index, SonarEcho echo) =>
            _echoes[index] = echo;

        public void RemoveEchoAt(int index) =>
            _echoes.RemoveAt(index);

        public void BeginFrame() =>
            _blips.Clear();

        public void AddBlip(SonarBlip blip) =>
            _blips.Add(blip);

        public void CommitFrame(SonarScopeFrame frame) {
            Frame = frame;
            OnFrameChanged?.Invoke();
        }
    }
}
