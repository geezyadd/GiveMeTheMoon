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
