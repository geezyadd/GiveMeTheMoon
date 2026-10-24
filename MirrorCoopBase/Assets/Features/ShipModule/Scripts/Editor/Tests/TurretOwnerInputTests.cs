using Features.CameraModule.Scripts.Models;
using Features.GrabModule.Scripts;
using Features.InputModule.Realization.Scripts.Generated;
using Features.ShipModule.Scripts.Turret;
using NUnit.Framework;
using UnityEngine;

namespace Features.ShipModule.Editor.Tests {
    // The seated owner's buttons: interact stands up unless the interaction already takes the press; a window with a
    // free cursor keeps the mouse off the turret.
    public sealed class TurretOwnerInputTests {
        private InputActions _inputActions;
        private InputService _inputService;
        private CursorModel _cursorModel;
        private HoveredInteractableModel _hoveredInteractableModel;
        private TurretOwnerInput _ownerInput;

        [SetUp]
        public void SetUp() {
            _inputActions = new InputActions();
            _inputService = new InputService(_inputActions);
            _cursorModel = new CursorModel();
            _hoveredInteractableModel = new HoveredInteractableModel();
            _ownerInput = new TurretOwnerInput(_inputService, _cursorModel, _hoveredInteractableModel);
            _ownerInput.Listen();
        }

        [TearDown]
        public void TearDown() {
            _ownerInput.StopListening();
            Object.DestroyImmediate(_inputActions.asset);
        }

        [Test]
        public void WhenInteractIsPressed_ThenOneStandUpIsRequested() {
            _inputService.Grab.Performed.Invoke();

            Assert.IsTrue(_ownerInput.ConsumeStandRequest());
            Assert.IsFalse(_ownerInput.ConsumeStandRequest());
        }

        [Test]
        public void WhenOwnerStopsListening_AndInteractIsPressed_ThenNothingIsRequested() {
            _ownerInput.StopListening();

            _inputService.Grab.Performed?.Invoke();

            Assert.IsFalse(_ownerInput.ConsumeStandRequest());
        }

        [Test]
        public void WhenWindowFreesTheCursor_AndMouseMoves_ThenTurretGetsNoLook() {
            _cursorModel.RequestFreeCursor();

            _inputService.Look.VectorChangedPerformed.Invoke(new Vector2(30f, 5f));

            Assert.AreEqual(Vector2.zero, _ownerInput.ConsumeLook());
            Assert.IsFalse(_ownerInput.IsFireHeld);
        }

        [Test]
        public void WhenFireIsPressedAndReleased_AndCursorIsFree_ThenTriggerIsNeverHeld() {
            _cursorModel.RequestFreeCursor();

            _inputService.Fire.Started.Invoke();
            Assert.IsFalse(_ownerInput.IsFireHeld);
            _inputService.Fire.Canceled.Invoke();
            Assert.IsFalse(_ownerInput.IsFireHeld);
        }
    }
}
