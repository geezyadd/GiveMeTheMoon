using Features.SonarModule.Scripts.Core;
using NUnit.Framework;
using UnityEngine;

namespace Features.SonarModule.Editor.Tests {
    public sealed class SonarMarkerProjectionTests {
        private const float TOLERANCE = 0.01f;

        private static readonly Vector2 _screen = new Vector2(1280f, 720f);

        private SonarTestConfiguration _configuration;
        private SonarMarkerProjection _projection;

        [SetUp]
        public void SetUp() {
            _configuration = new SonarTestConfiguration();
            _projection = new SonarMarkerProjection(_configuration.Configuration);
        }

        [TearDown]
        public void TearDown() =>
            _configuration.Dispose();

        [Test]
        public void WhenTheObjectIsOnScreen_ThenTheMarkerStaysOverIt() {
            SonarMarkerPlacement placement = _projection.Place(new Vector3(300f, 200f, 50f), _screen);

            Assert.IsTrue(placement.IsOnScreen);
            Assert.AreEqual(new Vector2(300f, 200f), placement.ScreenPosition);
        }

        [Test]
        public void WhenTheObjectIsOffToTheRight_ThenTheMarkerSitsOnTheRightEdgeWithTheArrowRight() {
            SonarMarkerPlacement placement = _projection.Place(new Vector3(5000f, 360f, 50f), _screen);

            Assert.IsFalse(placement.IsOnScreen);
            Assert.AreEqual(_screen.x - SonarTestConfiguration.MARGIN_PIXELS, placement.ScreenPosition.x, TOLERANCE);
            Assert.AreEqual(-90f, placement.ArrowDegrees, TOLERANCE);
        }

        [Test]
        public void WhenTheObjectIsBehindTheCamera_ThenTheMarkerPointsTheOtherWay() {
            SonarMarkerPlacement placement = _projection.Place(new Vector3(1000f, 360f, -50f), _screen);

            Assert.IsFalse(placement.IsOnScreen);
            Assert.AreEqual(SonarTestConfiguration.MARGIN_PIXELS, placement.ScreenPosition.x, TOLERANCE);
        }
    }
}
