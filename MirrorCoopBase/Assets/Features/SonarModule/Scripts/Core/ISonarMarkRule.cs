namespace Features.SonarModule.Scripts.Core {
    public interface ISonarMarkRule {
        public SonarMarkResult Evaluate(SonarMarkRequest request);
        public bool IsExpired(double expiresAt, double now);
    }
}
