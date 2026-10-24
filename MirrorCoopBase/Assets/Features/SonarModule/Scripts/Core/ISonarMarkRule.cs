using Features.SonarModule.Contracts;
using Features.SonarModule.Data;

namespace Features.SonarModule.Scripts.Core {
    public interface ISonarMarkRule {
        public SonarMarkResult Evaluate(SonarMarkRequest request);
        public SonarMark CreateMark(SonarContact contact, double now);
        public bool IsExpired(double expiresAt, double now);
    }
}
