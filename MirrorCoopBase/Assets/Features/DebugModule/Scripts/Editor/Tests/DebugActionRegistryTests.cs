using System;
using Features.DebugModule.Contracts;
using Features.DebugModule.Scripts.Models;
using Features.DebugModule.Scripts.Services;
using Features.DebugModule.Scripts.Systems;
using NUnit.Framework;

namespace Features.DebugModule.Editor.Tests {
    public sealed class DebugActionRegistryTests {
        private const string LABEL = "Hit the hull";
        private const string DESCRIPTION = "Damages the hull.";
        private const string REASON = "Host only.";
        private const float DEFAULT_DAMAGE = 10f;
        private const float TYPED_DAMAGE = 25f;

        private DebugActionsModel _debugActionsModel;
        private DebugPanelModel _debugPanelModel;
        private DebugActionRegistry _debugActionRegistry;
        private DebugActionRunService _debugActionRunService;
        private DebugActionAvailabilitySystem _debugActionAvailabilitySystem;
        private string _unavailableReason;
        private int _runCount;
        private float _lastValue;

        [SetUp]
        public void SetUp() {
            _debugActionsModel = new DebugActionsModel();
            _debugPanelModel = new DebugPanelModel();
            _debugActionRegistry = new DebugActionRegistry(_debugActionsModel);
            _debugActionRunService = new DebugActionRunService();
            _debugActionAvailabilitySystem = new DebugActionAvailabilitySystem(_debugActionsModel, _debugPanelModel);
            _unavailableReason = null;
            _runCount = 0;
            _lastValue = 0f;
        }

        [Test]
        public void WhenActionRegistered_ThenModelFindsItInItsSection() {
            _debugActionRegistry.Register(CreatePlainAction());

            Assert.IsTrue(_debugActionsModel.TryFind(DebugSections.SHIP, LABEL, out DebugActionDefinition action));
            Assert.AreEqual(DESCRIPTION, action.Description);
            Assert.IsFalse(_debugActionsModel.TryFind(DebugSections.ROCKS, LABEL, out _));
        }

        [Test]
        public void WhenRegistrationDisposedTwice_ThenActionIsGoneAndListChangedOnce() {
            IDisposable registration = _debugActionRegistry.Register(CreatePlainAction());
            int changes = 0;
            _debugActionsModel.OnActionsChanged += () => changes++;

            registration.Dispose();
            registration.Dispose();

            Assert.AreEqual(0, _debugActionsModel.Actions.Count);
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void WhenPanelClosed_ThenAvailabilityIsNotAsked() {
            _debugActionRegistry.Register(CreatePlainAction());
            _unavailableReason = REASON;

            _debugActionAvailabilitySystem.Tick();

            Assert.IsNull(_debugActionsModel.GetUnavailableReason(_debugActionsModel.Actions[0]));
        }

        [Test]
        public void WhenPanelOpenAndReasonChanges_ThenModelTellsOnceAndKeepsReason() {
            _debugActionRegistry.Register(CreatePlainAction());
            int changes = 0;
            _debugActionsModel.OnAvailabilityChanged += _ => changes++;
            _debugPanelModel.Toggle();
            _unavailableReason = REASON;

            _debugActionAvailabilitySystem.Tick();
            _debugActionAvailabilitySystem.Tick();

            Assert.AreEqual(REASON, _debugActionsModel.GetUnavailableReason(_debugActionsModel.Actions[0]));
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void WhenUnavailableActionRun_ThenCallbackIsNotCalled() {
            _debugActionRegistry.Register(CreatePlainAction());
            _unavailableReason = REASON;

            DebugActionRunResult result = _debugActionRunService.Run(_debugActionsModel.Actions[0], 0f);

            Assert.AreEqual(DebugActionRunResult.Unavailable, result);
            Assert.AreEqual(0, _runCount);
        }

        [Test]
        public void WhenValueActionRun_ThenCallbackGetsTypedValue() {
            DebugActionDefinition action = new(DebugSections.SHIP, LABEL, DESCRIPTION, ReadUnavailableReason, new DebugActionValue("Damage", DEFAULT_DAMAGE), RunWithValue);
            _debugActionRegistry.Register(action);

            DebugActionRunResult result = _debugActionRunService.Run(action, TYPED_DAMAGE);

            Assert.AreEqual(DebugActionRunResult.Ran, result);
            Assert.AreEqual(1, _runCount);
            Assert.AreEqual(TYPED_DAMAGE, _lastValue);
            Assert.AreEqual(DEFAULT_DAMAGE, action.Value.DefaultValue);
        }

        [Test]
        public void WhenPanelToggledTwice_ThenItIsClosedAgain() {
            _debugPanelModel.Toggle();
            Assert.IsTrue(_debugPanelModel.IsOpen);

            _debugPanelModel.Toggle();

            Assert.IsFalse(_debugPanelModel.IsOpen);
        }

        private DebugActionDefinition CreatePlainAction() =>
            new(DebugSections.SHIP, LABEL, DESCRIPTION, ReadUnavailableReason, Run);

        private string ReadUnavailableReason() =>
            _unavailableReason;

        private void Run() =>
            _runCount++;

        private void RunWithValue(float value) {
            _runCount++;
            _lastValue = value;
        }
    }
}
