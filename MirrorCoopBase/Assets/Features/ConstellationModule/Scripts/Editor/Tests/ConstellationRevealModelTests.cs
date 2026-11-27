using Features.ConstellationModule.Data;
using NUnit.Framework;

namespace Features.ConstellationModule.Editor.Tests {
    public sealed class ConstellationRevealModelTests {
        private const int ORION = 0;
        private const int BIG_DIPPER = 1;
        private const float TOLERANCE = 0.001f;

        private int _figureChanges;

        [Test]
        public void WhenTheModelIsNew_ThenNothingIsShown() {
            ConstellationRevealModel model = new();

            Assert.IsFalse(model.HasShown);
            Assert.IsFalse(model.HasFading);
        }

        [Test]
        public void WhenAConstellationIsShownWithADelay_ThenItsRevealStartsBelowZero() {
            ConstellationRevealModel model = new();

            model.Show(ORION, 0.3f);
            model.Advance(0.1f);

            Assert.AreEqual(ORION, model.ShownConstellation);
            Assert.AreEqual(-0.2f, model.ShownRevealSeconds, TOLERANCE);
        }

        [Test]
        public void WhenAnotherConstellationIsShown_ThenTheOldOneFadesFromWhereItsRevealWas() {
            ConstellationRevealModel model = new();
            model.Show(ORION, 0f);
            model.Advance(2f);

            model.Show(BIG_DIPPER, 0.3f);
            model.Advance(0.5f);

            Assert.AreEqual(BIG_DIPPER, model.ShownConstellation);
            Assert.AreEqual(ORION, model.FadingConstellation);
            Assert.AreEqual(2f, model.FadingRevealSeconds, TOLERANCE);
            Assert.AreEqual(0.5f, model.FadingSeconds, TOLERANCE);
        }

        [Test]
        public void WhenTheShownConstellationIsHidden_ThenItFadesAndNothingIsShown() {
            ConstellationRevealModel model = new();
            model.Show(ORION, 0f);
            _figureChanges = 0;
            model.OnFiguresChanged += OnFiguresChanged;

            model.Hide();
            model.Hide();
            model.OnFiguresChanged -= OnFiguresChanged;

            Assert.IsFalse(model.HasShown);
            Assert.AreEqual(ORION, model.FadingConstellation);
            Assert.AreEqual(1, _figureChanges);
        }

        [Test]
        public void WhenTheFadeEnds_ThenNoFigureFades() {
            ConstellationRevealModel model = new();
            model.Show(ORION, 0f);
            model.Hide();

            model.EndFading();

            Assert.IsFalse(model.HasFading);
        }

        private void OnFiguresChanged() =>
            _figureChanges++;
    }
}
