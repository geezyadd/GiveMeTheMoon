using System;

namespace Features.SonarModule.Data {
    // What the navigator asks for at the console (the window and the keys): a ping, a mark, closing the console.
    public sealed class SonarConsoleRequests {
        public event Action OnPingRequested;
        public event Action<string> OnMarkRequested;
        public event Action OnCloseRequested;

        public void RequestPing() =>
            OnPingRequested?.Invoke();

        public void RequestMark(string contactId) =>
            OnMarkRequested?.Invoke(contactId);

        public void RequestClose() =>
            OnCloseRequested?.Invoke();
    }
}
