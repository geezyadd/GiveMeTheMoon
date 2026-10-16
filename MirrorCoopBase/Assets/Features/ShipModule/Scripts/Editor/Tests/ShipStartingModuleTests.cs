using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.ShipModule.Scripts.Editor.Tests {
    public sealed class ShipStartingModuleTests {
        // ShipPlatform.prefab, the ship every run starts with.
        private const string SHIP_PREFAB_GUID = "d5f9b02c3e4a51b6c7d8e9f0a1b2c3d4";
        private const string STARTING_MODULE_FIELD = "_startingModule";
        private const string MODULE_ID = "starting-module";
        private const int LOOP_INDEX = 0;

        [Test]
        public void WhenShipPrefabLoaded_ThenAControlSocketStartsWithANonRemovableControlModule() {
            int controlSockets = 0;
            foreach (ShipSocket socket in LoadShipSockets()) {
                if (socket.AcceptedType != ShipModuleType.Control)
                    continue;

                controlSockets++;
                ModuleDefinition module = ReadStartingModule(socket);
                Assert.IsNotNull(module, socket.name + " has no starting module: the ship would start without a helm.");
                Assert.IsTrue(module.IsControlModule, socket.name + " starts with a module the pilot cannot steer with.");
                Assert.IsFalse(module.IsRemovable, socket.name + " starts with a module the crew can take out.");
            }

            Assert.AreEqual(1, controlSockets, "The ship should have exactly one control socket.");
        }

        [Test]
        public void WhenSocketHasAStartingModule_ThenTheResetInstallsIt() {
            ShipSocketState state = new ShipSocketRule(ShipModuleType.Control, LOOP_INDEX, MODULE_ID).CreateResetState();

            Assert.IsTrue(state.Occupied, "The reset left the socket empty.");
            Assert.AreEqual(MODULE_ID, state.ModuleId);
            Assert.AreEqual(0u, state.OccupantNetId, "The reset kept a seated rider.");
        }

        [Test]
        public void WhenSocketHasNoStartingModule_ThenTheResetEmptiesIt() =>
            Assert.AreEqual(default(ShipSocketState), new ShipSocketRule(ShipModuleType.Engine, LOOP_INDEX, null).CreateResetState());

        [Test]
        public void WhenStartingModuleIsNotRemovable_ThenItCannotBeUninstalledInBuild() {
            ShipSocketRule rule = new ShipSocketRule(ShipModuleType.Control, LOOP_INDEX, MODULE_ID);

            Assert.IsFalse(rule.CanUninstall(rule.CreateResetState().Occupied, false, new ShipSocketRunState(LOOP_INDEX, ShipRunPhase.Build)));
        }

        private static ShipSocket[] LoadShipSockets() {
            GameObject ship = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(SHIP_PREFAB_GUID));
            Assert.IsNotNull(ship, "The ship prefab was not found.");
            return ship.GetComponentsInChildren<ShipSocket>(true);
        }

        private static ModuleDefinition ReadStartingModule(ShipSocket socket) =>
            (ModuleDefinition)new SerializedObject(socket).FindProperty(STARTING_MODULE_FIELD).objectReferenceValue;
    }
}
