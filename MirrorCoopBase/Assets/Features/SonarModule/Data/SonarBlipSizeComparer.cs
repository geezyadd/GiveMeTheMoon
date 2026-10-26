using System.Collections.Generic;

namespace Features.SonarModule.Data {
    // The biggest echo first, so a small echo is drawn over a giant's disc and takes the click.
    public sealed class SonarBlipSizeComparer : IComparer<SonarBlip> {
        public int Compare(SonarBlip first, SonarBlip second) =>
            second.RadiusShare.CompareTo(first.RadiusShare);
    }
}
