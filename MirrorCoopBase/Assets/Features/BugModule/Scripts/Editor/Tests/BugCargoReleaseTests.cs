using Features.BugModule.Scripts.Brain;
using Features.BugModule.Scripts.Health;
using NUnit.Framework;
using UnityEngine;

namespace Features.BugModule.Scripts.Editor.Tests {
    // A bug carrying a cargo lets it go however it ends: shot down where it flies, its agent removed from outside or
    // the ship gone at the point it was last; a bug without a cargo lets nothing go.
    public sealed class BugCargoReleaseTests {
        private const int CARRIER_ID = 3;
        private const int OTHER_ID = 4;
        private const float HEALTH = 10f;

        private static readonly Vector3 _flyingAt = new(10f, 2f, 40f);
        private static readonly Vector3 _lastSeenAt = new(-5f, 1f, 30f);

        private BugServerModel _model;
        private VanishingFieldAgentControl _control;
        private BugRemovalService _removal;
        private FakeCargoTarget _cargo;

        [SetUp]
        public void SetUp() {
            _model = new BugServerModel();
            _control = new VanishingFieldAgentControl();
            _removal = new BugRemovalService(_model, _control, new SilentBugDeathBroadcast());
            _cargo = new FakeCargoTarget(Vector3.zero);
            BugAgent carrier = new(CARRIER_ID, 0f, HEALTH);
            carrier.GoForCargo(_cargo);
            carrier.CarryCargo();
            carrier.RememberWorldPosition(_lastSeenAt);
            _model.Add(carrier);
            _model.Add(new BugAgent(OTHER_ID, 0f, HEALTH));
            _control.Fly(CARRIER_ID, _flyingAt);
            _control.Fly(OTHER_ID, _flyingAt);
        }

        [Test]
        public void WhenACarrierIsShotDown_ThenItsCargoIsLetGoWhereItFlies() {
            _removal.ServerKill(CARRIER_ID);

            Assert.IsTrue(_cargo.IsReleased);
            Assert.AreEqual(_flyingAt, _cargo.LastCarrierPosition);
            Assert.IsFalse(_model.TryGet(CARRIER_ID, out _));
        }

        [Test]
        public void WhenACarriersAgentIsRemovedFromOutside_ThenItsCargoIsLetGoWhereItWasLast() {
            _control.Vanish(CARRIER_ID);
            _removal.ServerForgetVanished(CARRIER_ID);

            Assert.IsTrue(_cargo.IsReleased);
            Assert.AreEqual(_lastSeenAt, _cargo.LastCarrierPosition);
            Assert.IsFalse(_model.TryGet(CARRIER_ID, out _));
            Assert.IsEmpty(_control.Despawned);
        }

        [Test]
        public void WhenACarrierIsKilledButItsAgentCannotSayWhereItIs_ThenItsCargoIsLetGoWhereItWasLast() {
            _control.Vanish(CARRIER_ID);
            _removal.ServerKill(CARRIER_ID);

            Assert.IsTrue(_cargo.IsReleased);
            Assert.AreEqual(_lastSeenAt, _cargo.LastCarrierPosition);
        }

        [Test]
        public void WhenTheShipIsGone_ThenEveryBugIsForgottenAndItsCargoLetGo() {
            _removal.ServerForgetAll();

            Assert.IsTrue(_cargo.IsReleased);
            Assert.AreEqual(0, _model.Count);
        }

        [Test]
        public void WhenABugWithoutACargoIsForgotten_ThenNoCargoIsLetGo() {
            _removal.ServerForgetVanished(OTHER_ID);

            Assert.IsFalse(_cargo.IsReleased);
            Assert.IsTrue(_model.TryGet(CARRIER_ID, out _));
        }
    }
}
