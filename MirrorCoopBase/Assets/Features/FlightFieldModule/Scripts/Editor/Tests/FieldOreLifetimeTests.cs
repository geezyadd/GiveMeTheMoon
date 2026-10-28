using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Ore;
using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // How long a piece of ore lasts and where it is meanwhile: it drifts at its velocity from the drop, fades in its
    // last seconds, is gone at its kind's lifetime on the server, and a piece on a rope does not run out.
    public sealed class FieldOreLifetimeTests {
        private const string WEAPON_ID = "harpoon";
        private const float PULL_SPEED = 20f;
        private const float MAX_ROPE = 80f;

        private static readonly PlayerKey _hooker = new("client:hooker");

        private FieldOreTestRig _rig;

        [SetUp]
        public void SetUp() =>
            _rig = new FieldOreTestRig(null);

        [Test]
        public void WhenOreDrifts_ThenItMovesAtItsVelocityFromTheDrop() {
            Vector3 start = new(5f, 2f, 30f);
            Vector3 velocity = new(1f, 0f, -0.5f);
            FieldOreId id = _rig.Spawn(start, velocity);

            _rig.Clock.Now = 4d;

            Assert.That(Vector3.Distance(start + velocity * 4f, _rig.Describe(id).Center), Is.LessThan(0.001f));
        }

        [Test]
        public void WhenOreOutlivesItsLifetime_ThenTheServerRemovesItAndNotBefore() {
            FieldOreId id = _rig.Spawn(Vector3.zero, Vector3.zero);
            float lifetime = _rig.FirstKind.LifetimeSeconds;

            _rig.Clock.Now = lifetime - 0.1f;
            _rig.Expiry.ServerRemoveExpired();
            Assert.IsTrue(_rig.Query.TryGetOre(id, out _), "A piece is gone before its lifetime.");

            _rig.Clock.Now = lifetime;
            _rig.Expiry.ServerRemoveExpired();
            Assert.IsFalse(_rig.Query.TryGetOre(id, out _), "A piece outlives its lifetime.");
        }

        [Test]
        public void WhenOreNearsItsEnd_ThenItFadesOutOverTheFadeSeconds() {
            _rig.Spawn(Vector3.zero, Vector3.zero);
            float lifetime = _rig.FirstKind.LifetimeSeconds;
            float fade = _rig.Configuration.FadeSeconds;

            Assert.AreEqual(1f, _rig.Lifetime.GetVisibleShare(_rig.Ores.Get(0), lifetime - fade - 1f), "A piece fades before its fade time.");
            Assert.AreEqual(0.5f, _rig.Lifetime.GetVisibleShare(_rig.Ores.Get(0), lifetime - fade * 0.5f), 0.001f, "A piece is not half faded halfway through.");
            Assert.AreEqual(0f, _rig.Lifetime.GetVisibleShare(_rig.Ores.Get(0), lifetime), "A piece still shines at its end of life.");
        }

        [Test]
        public void WhenOreIsOnARope_ThenItDoesNotRunOut() {
            _rig.Ship.Position = new Vector3(0f, 0f, -400f);
            FieldOreId id = _rig.Spawn(Vector3.zero, Vector3.zero);
            _rig.Hooks.ServerHook(id, _hooker, WEAPON_ID, Vector3.zero, new WeaponPull(PULL_SPEED, MAX_ROPE));

            _rig.Clock.Now = _rig.FirstKind.LifetimeSeconds + 1f;
            _rig.Expiry.ServerRemoveExpired();

            Assert.IsTrue(_rig.Query.TryGetOre(id, out _), "A piece on a rope ran out.");
        }
    }
}
