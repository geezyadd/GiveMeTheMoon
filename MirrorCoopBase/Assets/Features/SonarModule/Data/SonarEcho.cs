using Features.SonarModule.Contracts;

namespace Features.SonarModule.Data {
    // A contact the ping wave reached: where it was then and when the wave lit it.
    public readonly struct SonarEcho {
        public SonarContact Contact { get; }
        public float LitAt { get; }

        public SonarEcho(SonarContact contact, float litAt) {
            Contact = contact;
            LitAt = litAt;
        }
    }
}
