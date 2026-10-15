#if UNITY_INCLUDE_TESTS
using Features.ShipModule.Scripts;
using NUnit.Framework;
using Tests.PlayMode.LoopSmoke;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSoak {
    // What must not pile up from loop to loop: pads beyond the current and the previous one, wreckage, and runtime
    // network objects other than the station items. The items grow by design and are only reported.
    public sealed class LoopSoakAccumulationCheck {
        private const int MAX_PADS = 2;
        private const string CLONE_SUFFIX = "(Clone)";

        private readonly string _wreckName = LoopSmokeShopCheck.Resolve<ShipStationCatalog>().WreckPrefab.name + CLONE_SUFFIX;
        private LoopSoakSpawnCensus _firstLoop;
        private bool _hasFirstLoop;

        public void AssertNothingPiledUp(int loop, LoopSoakSpawnCensus census) {
            Assert.LessOrEqual(census.Pads, MAX_PADS, $"Loop {loop}: {census.Pads} landing pads are spawned, more than the current and the previous one.");
            Assert.LessOrEqual(LoopSmokeSpawnCounts.CountSceneObjects<ShipLandingPad>(), MAX_PADS, $"Loop {loop}: more landing pads in the scene than the current and the previous one.");
            Assert.AreEqual(0, CountWrecks(), $"Loop {loop}: wreckage lies in the scene after a normal landing.");
            if (_hasFirstLoop == false) {
                _firstLoop = census;
                _hasFirstLoop = true;
                return;
            }

            Assert.AreEqual(_firstLoop.ServerOther, census.ServerOther, $"Loop {loop}: server runtime objects other than items and pads changed since loop 1.");
            Assert.AreEqual(_firstLoop.ClientOther, census.ClientOther, $"Loop {loop}: client runtime objects other than items and pads changed since loop 1.");
        }

        private int CountWrecks() {
            int count = 0;
            foreach (Transform root in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) {
                if (root.parent == null && root.name == _wreckName)
                    count++;
            }

            return count;
        }
    }
}
#endif
