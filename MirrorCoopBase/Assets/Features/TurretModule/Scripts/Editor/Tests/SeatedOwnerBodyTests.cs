using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Features.TurretModule.Scripts.Editor.Tests {
    // The seated gunner's own body is hidden from its own view through the gun, keeps its shadow, and comes back as it was.
    public sealed class SeatedOwnerBodyTests {
        private GameObject _occupant;
        private MeshRenderer _body;
        private MeshRenderer _nameplate;

        [SetUp]
        public void SetUp() {
            _occupant = new GameObject(nameof(SeatedOwnerBodyTests));
            _body = new GameObject().AddComponent<MeshRenderer>();
            _body.transform.SetParent(_occupant.transform);
            _nameplate = new GameObject().AddComponent<MeshRenderer>();
            _nameplate.transform.SetParent(_occupant.transform);
            _nameplate.shadowCastingMode = ShadowCastingMode.Off;
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_occupant);

        [Test]
        public void WhenHidden_ThenEveryRendererCastsOnlyItsShadow() {
            SeatedOwnerBody body = new SeatedOwnerBody();

            body.Hide(_occupant.transform);

            Assert.AreEqual(ShadowCastingMode.ShadowsOnly, _body.shadowCastingMode);
            Assert.AreEqual(ShadowCastingMode.ShadowsOnly, _nameplate.shadowCastingMode);
        }

        [Test]
        public void WhenHidden_AndShown_ThenEveryRendererIsAsBefore() {
            SeatedOwnerBody body = new SeatedOwnerBody();

            body.Hide(_occupant.transform);
            body.Show();

            Assert.AreEqual(ShadowCastingMode.On, _body.shadowCastingMode);
            Assert.AreEqual(ShadowCastingMode.Off, _nameplate.shadowCastingMode);
        }

        [Test]
        public void WhenHiddenTwice_AndShown_ThenEveryRendererIsAsBefore() {
            SeatedOwnerBody body = new SeatedOwnerBody();

            body.Hide(_occupant.transform);
            body.Hide(_occupant.transform);
            body.Show();

            Assert.AreEqual(ShadowCastingMode.On, _body.shadowCastingMode);
            Assert.AreEqual(ShadowCastingMode.Off, _nameplate.shadowCastingMode);
        }

        [Test]
        public void WhenARendererIsDestroyed_AndShown_ThenTheOthersAreRestored() {
            SeatedOwnerBody body = new SeatedOwnerBody();
            body.Hide(_occupant.transform);

            Object.DestroyImmediate(_nameplate.gameObject);
            body.Show();

            Assert.AreEqual(ShadowCastingMode.On, _body.shadowCastingMode);
        }
    }
}
