namespace Features.SonarModule.Scripts.Core {
    // What the server knows when the navigator asks to mark a contact.
    public readonly struct SonarMarkRequest {
        public bool IsNavigator { get; }
        public bool IsContactFound { get; }
        public bool IsMarkable { get; }
        public bool IsAlreadyMarked { get; }
        public int ActiveMarkCount { get; }

        public SonarMarkRequest(bool isNavigator, bool isContactFound, bool isMarkable, bool isAlreadyMarked, int activeMarkCount) {
            IsNavigator = isNavigator;
            IsContactFound = isContactFound;
            IsMarkable = isMarkable;
            IsAlreadyMarked = isAlreadyMarked;
            ActiveMarkCount = activeMarkCount;
        }
    }
}
