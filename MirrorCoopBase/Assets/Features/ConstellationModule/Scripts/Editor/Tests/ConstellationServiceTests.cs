using System;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.ConstellationModule.Data;
using Features.ConstellationModule.Scripts.Configurations;
using Features.ConstellationModule.Scripts.Core;
using Features.ConstellationModule.Scripts.Definitions;
using NUnit.Framework;

namespace Features.ConstellationModule.Editor.Tests {
    // The server's sky is written through the bound writer only, and with catalog ids only.
    public sealed class ConstellationServiceTests {
        private const string ORION = "orion";
        private const string UNKNOWN = "no_such_constellation";
        private const string ALL = ConstellationSkyIds.ALL_CONSTELLATIONS;

        private ConstellationSkyBindingModel _bindingModel;
        private FakeConstellationSkyWriter _writer;
        private ConstellationService _service;

        [SetUp]
        public void SetUp() {
            ConstellationCatalog catalog = AddressableConfigurationLoader.Load<ConstellationCatalog>(Address.Configurations.ConstellationCatalog_Default);
            ConstellationHighlightConfiguration configuration = AddressableConfigurationLoader.Load<ConstellationHighlightConfiguration>(Address.Configurations.ConstellationHighlightConfiguration_Default);
            _bindingModel = new ConstellationSkyBindingModel();
            _writer = new FakeConstellationSkyWriter();
            _service = new ConstellationService(_bindingModel, catalog, new ConstellationSkyStateRule(new ConstellationFadeRule(configuration)));
        }

        [Test]
        public void WhenAConstellationIsShown_ThenTheSkyShowsItsId() {
            _bindingModel.Bind(_writer);

            _service.ServerShow(ORION);

            Assert.AreEqual(ORION, _writer.State.ConstellationId);
        }

        [Test]
        public void WhenAllAreShown_ThenTheSkyShowsTheAllId() {
            _bindingModel.Bind(_writer);

            _service.ServerShowAll();

            Assert.AreEqual(ALL, _writer.State.ConstellationId);
        }

        [Test]
        public void WhenAllAreShownAndHidden_ThenTheSkyShowsNothing() {
            _bindingModel.Bind(_writer);
            _service.ServerShowAll();

            _service.ServerHide();

            Assert.AreEqual(string.Empty, _writer.State.ConstellationId);
            Assert.AreEqual(2, _writer.Writes);
        }

        [Test]
        public void WhenTheAllIdIsShownAsOneConstellation_ThenItThrowsAndNothingIsWritten() {
            _bindingModel.Bind(_writer);

            Assert.Throws<ArgumentException>(() => _service.ServerShow(ALL));
            Assert.AreEqual(0, _writer.Writes);
        }

        [Test]
        public void WhenNoWriterIsBound_ThenShowingAllThrows() =>
            Assert.Throws<InvalidOperationException>(() => _service.ServerShowAll());

        [Test]
        public void WhenAnEmptySkyIsHidden_ThenNothingIsWritten() {
            _bindingModel.Bind(_writer);

            _service.ServerHide();

            Assert.AreEqual(0, _writer.Writes);
        }

        [Test]
        public void WhenAnIdOutsideTheCatalogIsShown_ThenItThrowsAndNothingIsWritten() {
            _bindingModel.Bind(_writer);

            Assert.Throws<ArgumentException>(() => _service.ServerShow(UNKNOWN));
            Assert.AreEqual(0, _writer.Writes);
        }

        [Test]
        public void WhenNoWriterIsBound_ThenShowingThrows() =>
            Assert.Throws<InvalidOperationException>(() => _service.ServerShow(ORION));
    }
}
