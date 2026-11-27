using Features.ConstellationModule.Data;
using NUnit.Framework;

namespace Features.ConstellationModule.Editor.Tests {
    public sealed class ConstellationRevealModelTests {
        private const int ORION = 0;
        private const int BIG_DIPPER = 1;
        private const int ALL = ConstellationRevealModel.ALL_CONSTELLATIONS;
        private const double START_TIME = 100d;
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

            model.Show(ORION, START_TIME + 0.3d, START_TIME);
            model.SetTime(START_TIME + 0.1d);

            Assert.AreEqual(ORION, model.ShownConstellation);
            Assert.AreEqual(-0.2f, model.ShownRevealSeconds, TOLERANCE);
        }

        [Test]
        public void WhenAConstellationWasShownLongAgo_ThenItsRevealIsAsFarAsTheTimeSinceItsStart() {
            ConstellationRevealModel model = new();

            model.Show(ORION, START_TIME, START_TIME);
            model.SetTime(START_TIME + 60d);

            Assert.AreEqual(60f, model.ShownRevealSeconds, TOLERANCE);
            Assert.IsFalse(model.HasFading);
        }

        [Test]
        public void WhenAnotherConstellationIsShown_ThenTheOldOneFadesFromWhereItsRevealWas() {
            ConstellationRevealModel model = new();
            model.Show(ORION, START_TIME, START_TIME);

            model.Show(BIG_DIPPER, START_TIME + 2.3d, START_TIME + 2d);
            model.SetTime(START_TIME + 2.5d);

            Assert.AreEqual(BIG_DIPPER, model.ShownConstellation);
            Assert.AreEqual(ORION, model.FadingConstellation);
            Assert.AreEqual(2f, model.FadingRevealSeconds, TOLERANCE);
            Assert.AreEqual(0.5f, model.FadingSeconds, TOLERANCE);
        }

        [Test]
        public void WhenTheSameRevealIsToldAgain_ThenNothingChanges() {
            ConstellationRevealModel model = new();
            model.Show(ORION, START_TIME, START_TIME);
            _figureChanges = 0;
            model.OnFiguresChanged += OnFiguresChanged;

            model.Show(ORION, START_TIME, START_TIME);
            model.OnFiguresChanged -= OnFiguresChanged;

            Assert.AreEqual(0, _figureChanges);
            Assert.IsFalse(model.HasFading);
        }

        [Test]
        public void WhenTheShownConstellationIsReplayed_ThenItFadesAndIsShownAgain() {
            ConstellationRevealModel model = new();
            model.Show(ORION, START_TIME, START_TIME);

            model.Show(ORION, START_TIME + 3.8d, START_TIME + 3d);

            Assert.AreEqual(ORION, model.ShownConstellation);
            Assert.AreEqual(ORION, model.FadingConstellation);
            Assert.AreEqual(3f, model.FadingRevealSeconds, TOLERANCE);
        }

        [Test]
        public void WhenTheShownConstellationIsHidden_ThenItFadesAndNothingIsShown() {
            ConstellationRevealModel model = new();
            model.Show(ORION, START_TIME, START_TIME);
            _figureChanges = 0;
            model.OnFiguresChanged += OnFiguresChanged;

            model.Hide(START_TIME + 1d);
            model.Hide(START_TIME + 1d);
            model.OnFiguresChanged -= OnFiguresChanged;

            Assert.IsFalse(model.HasShown);
            Assert.AreEqual(ORION, model.FadingConstellation);
            Assert.AreEqual(1, _figureChanges);
        }

        [Test]
        public void WhenTheFadeEnds_ThenNoFigureFades() {
            ConstellationRevealModel model = new();
            model.Show(ORION, START_TIME, START_TIME);
            model.Hide(START_TIME + 1d);

            model.EndFading();

            Assert.IsFalse(model.HasFading);
        }

        [Test]
        public void WhenAllAreShown_ThenAllAreShownFromTheirStart() {
            ConstellationRevealModel model = new();

            model.Show(ALL, START_TIME, START_TIME);
            model.SetTime(START_TIME + 0.5d);

            Assert.IsTrue(model.HasShown);
            Assert.AreEqual(ALL, model.ShownConstellation);
            Assert.AreEqual(0.5f, model.ShownRevealSeconds, TOLERANCE);
        }

        [Test]
        public void WhenAllWereShownBeforeAJoin_ThenTheirRevealIsAsFarAsTheTimeSinceTheirStart() {
            ConstellationRevealModel model = new();

            model.Show(ALL, START_TIME, START_TIME);
            model.SetTime(START_TIME + 3.5d);

            Assert.AreEqual(3.5f, model.ShownRevealSeconds, TOLERANCE);
            Assert.IsFalse(model.HasFading);
        }

        [Test]
        public void WhenOneIsShownWhileAllAreShown_ThenAllFadeFromWhereTheirRevealWas() {
            ConstellationRevealModel model = new();
            model.Show(ALL, START_TIME, START_TIME);

            model.Show(ORION, START_TIME + 2.3d, START_TIME + 2d);

            Assert.AreEqual(ORION, model.ShownConstellation);
            Assert.AreEqual(ALL, model.FadingConstellation);
            Assert.AreEqual(2f, model.FadingRevealSeconds, TOLERANCE);
        }

        [Test]
        public void WhenAllAreShownWhileOneIsShown_ThenItFadesAndAllAreShown() {
            ConstellationRevealModel model = new();
            model.Show(ORION, START_TIME, START_TIME);

            model.Show(ALL, START_TIME + 2.3d, START_TIME + 2d);

            Assert.AreEqual(ALL, model.ShownConstellation);
            Assert.AreEqual(ORION, model.FadingConstellation);
        }

        [Test]
        public void WhenAllAreReplayed_ThenAllFadeAndAreShownAgain() {
            ConstellationRevealModel model = new();
            model.Show(ALL, START_TIME, START_TIME);

            model.Show(ALL, START_TIME + 3.8d, START_TIME + 3d);
            model.SetTime(START_TIME + 3.8d);

            Assert.AreEqual(ALL, model.ShownConstellation);
            Assert.AreEqual(ALL, model.FadingConstellation);
            Assert.AreEqual(0f, model.ShownRevealSeconds, TOLERANCE);
        }

        [Test]
        public void WhenAllAreHidden_ThenAllFadeAndNothingIsShown() {
            ConstellationRevealModel model = new();
            model.Show(ALL, START_TIME, START_TIME);

            model.Hide(START_TIME + 1d);

            Assert.IsFalse(model.HasShown);
            Assert.AreEqual(ALL, model.FadingConstellation);
        }

        private void OnFiguresChanged() =>
            _figureChanges++;
    }
}
