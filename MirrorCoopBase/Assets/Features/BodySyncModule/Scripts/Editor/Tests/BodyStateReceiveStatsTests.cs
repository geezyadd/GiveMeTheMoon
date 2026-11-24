using Features.BodySyncModule.Receiving;
using NUnit.Framework;

namespace Features.BodySyncModule.Editor.Tests {
    public sealed class BodyStateReceiveStatsTests {
        private const double TOLERANCE = 0.0001d;

        private BodyStateReceiveStats _stats;

        [SetUp]
        public void SetUp() =>
            _stats = new BodyStateReceiveStats();

        [Test]
        public void WhenPacketsArrive_ThenTheReportCountsThemAndTheirAge() {
            _stats.RecordAccepted(0.1d, 0d);
            _stats.RecordAccepted(0.2d, 1d);
            _stats.RecordDropped(1.5d);

            BodyStateReceiveReport report = _stats.TakeReport(2d);

            Assert.AreEqual(2, report.Accepted);
            Assert.AreEqual(1, report.Dropped);
            Assert.AreEqual(1d, report.PacketsPerSecond, TOLERANCE);
            Assert.AreEqual(0.15d, report.AverageAgeSeconds, TOLERANCE);
            Assert.AreEqual(0.2d, report.LastAgeSeconds, TOLERANCE);
        }

        [Test]
        public void WhenAReportIsTaken_ThenTheNextOneStartsFromZero() {
            _stats.RecordAccepted(0.1d, 0d);
            _stats.TakeReport(1d);
            _stats.RecordAccepted(0.3d, 1.5d);

            BodyStateReceiveReport report = _stats.TakeReport(2d);

            Assert.AreEqual(1, report.Accepted);
            Assert.AreEqual(0, report.Dropped);
            Assert.AreEqual(1d, report.PacketsPerSecond, TOLERANCE);
            Assert.AreEqual(0.3d, report.AverageAgeSeconds, TOLERANCE);
        }

        [Test]
        public void WhenNothingArrived_ThenTheReportIsEmpty() {
            BodyStateReceiveReport report = _stats.TakeReport(5d);

            Assert.AreEqual(0, report.Accepted);
            Assert.AreEqual(0d, report.PacketsPerSecond, TOLERANCE);
        }
    }
}
