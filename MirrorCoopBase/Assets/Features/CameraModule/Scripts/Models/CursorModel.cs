using System;

namespace Features.CameraModule.Scripts.Models {
    public sealed class CursorModel {
        private const string UNMATCHED_RELEASE_MESSAGE = "ReleaseFreeCursor called without a matching RequestFreeCursor.";

        private int _freeCursorRequests;

        public bool IsFreeCursorRequested => _freeCursorRequests > 0;

        public void RequestFreeCursor() =>
            _freeCursorRequests++;

        public void ReleaseFreeCursor() {
            if (_freeCursorRequests == 0)
                throw new InvalidOperationException(UNMATCHED_RELEASE_MESSAGE);

            _freeCursorRequests--;
        }
    }
}
