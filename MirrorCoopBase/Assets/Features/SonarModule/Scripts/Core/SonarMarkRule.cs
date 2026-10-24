using Features.SonarModule.Scripts.Configurations;

namespace Features.SonarModule.Scripts.Core {
    // The server's check of a mark: only the navigator at the console marks, only a contact the sonar really has and
    // whose kind may be marked, and no more marks than the limit. Marking a marked contact again renews its time.
    public sealed class SonarMarkRule : ISonarMarkRule {
        private readonly SonarConfiguration _sonarConfiguration;

        public SonarMarkRule(SonarConfiguration sonarConfiguration) =>
            _sonarConfiguration = sonarConfiguration;

        public SonarMarkResult Evaluate(SonarMarkRequest request) {
            if (request.IsNavigator == false)
                return SonarMarkResult.NotNavigator;

            if (request.IsContactFound == false)
                return SonarMarkResult.UnknownContact;

            if (request.IsMarkable == false)
                return SonarMarkResult.NotMarkable;

            if (request.IsAlreadyMarked)
                return SonarMarkResult.Refreshed;

            return request.ActiveMarkCount >= _sonarConfiguration.MarkLimit ? SonarMarkResult.LimitReached : SonarMarkResult.Marked;
        }

        public bool IsExpired(double expiresAt, double now) =>
            now >= expiresAt;
    }
}
