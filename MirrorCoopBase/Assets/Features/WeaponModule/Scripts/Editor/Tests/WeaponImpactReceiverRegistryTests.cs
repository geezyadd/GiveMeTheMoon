using System.Collections.Generic;
using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Impacts;
using NUnit.Framework;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    // A body that reacts to shots is found by any of its parts and by its network id while it is listed.
    public sealed class WeaponImpactReceiverRegistryTests {
        private const uint BODY_ID = 7;

        private GameObject _head;
        private GameObject _chest;
        private TestReceiver _receiver;

        [SetUp]
        public void SetUp() {
            _head = new GameObject("Head", typeof(Rigidbody));
            _chest = new GameObject("Chest", typeof(Rigidbody));
            _receiver = new TestReceiver(BODY_ID, _head.GetComponent<Rigidbody>(), _chest.GetComponent<Rigidbody>());
        }

        [TearDown]
        public void TearDown() {
            Object.DestroyImmediate(_head);
            Object.DestroyImmediate(_chest);
        }

        [Test]
        public void WhenABodyIsAdded_ThenItsPartAndIdFindIt() {
            WeaponImpactReceiverRegistry registry = new WeaponImpactReceiverRegistry();
            registry.Add(_receiver);

            Assert.IsTrue(registry.TryFindByPart(_chest.GetComponent<Rigidbody>(), out IWeaponImpactReceiver byPart, out int partIndex));
            Assert.AreSame(_receiver, byPart);
            Assert.AreEqual(1, partIndex);
            Assert.IsTrue(registry.TryGet(BODY_ID, out IWeaponImpactReceiver byId));
            Assert.AreSame(_receiver, byId);
        }

        [Test]
        public void WhenABodyIsRemoved_ThenNothingFindsIt() {
            WeaponImpactReceiverRegistry registry = new WeaponImpactReceiverRegistry();
            registry.Add(_receiver);
            registry.Remove(_receiver);

            Assert.IsFalse(registry.TryFindByPart(_head.GetComponent<Rigidbody>(), out _, out _));
            Assert.IsFalse(registry.TryGet(BODY_ID, out _));
        }

        private sealed class TestReceiver : IWeaponImpactReceiver {
            private readonly Rigidbody[] _parts;

            public TestReceiver(uint networkId, params Rigidbody[] parts) {
                NetworkId = networkId;
                _parts = parts;
            }

            public uint NetworkId { get; }
            public IReadOnlyList<Rigidbody> Parts => _parts;

            public void ReceiveImpact(in WeaponImpact impact) {
            }
        }
    }
}
