using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipSocketIdTests {
        // ShipPlatform.prefab: the ShipSockets model keys every socket of this ship by its id.
        private const string SHIP_PREFAB_GUID = "d5f9b02c3e4a51b6c7d8e9f0a1b2c3d4";

        private ShipSocket[] _sockets;

        [SetUp]
        public void SetUp() {
            GameObject ship = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(SHIP_PREFAB_GUID));
            Assert.IsNotNull(ship, "The ship prefab was not found.");
            _sockets = ship.GetComponentsInChildren<ShipSocket>(true);
        }

        [Test]
        public void WhenShipPrefabLoaded_ThenItHasSockets() =>
            Assert.IsNotEmpty(_sockets);

        [Test]
        public void WhenShipPrefabLoaded_ThenEverySocketHasAnId() {
            for (int i = 0; i < _sockets.Length; i++)
                Assert.IsFalse(string.IsNullOrWhiteSpace(_sockets[i].SocketId), _sockets[i].name + " has no socket id.");
        }

        [Test]
        public void WhenShipPrefabLoaded_ThenSocketIdsAreUnique() {
            HashSet<string> ids = new HashSet<string>();
            for (int i = 0; i < _sockets.Length; i++)
                Assert.IsTrue(ids.Add(_sockets[i].SocketId), _sockets[i].name + " repeats socket id " + _sockets[i].SocketId + ".");
        }
    }
}
