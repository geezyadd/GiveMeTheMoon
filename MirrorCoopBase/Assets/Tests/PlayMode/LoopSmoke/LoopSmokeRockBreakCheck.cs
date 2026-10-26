#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Structures;
using Features.NetworkModelModule.Scripts;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // The breakable rock base on the host: in cruise the debug entry breaks the rock nearest to the ship; it is in the
    // synced broken list, RockDestroyed fired once (a second break is refused), and the rock is hidden on this peer.
    // After landing the broken list is empty.
    public sealed class LoopSmokeRockBreakCheck {
        private readonly IFieldRockBreakDebug _fieldRockBreakDebug;
        private readonly IFieldRockBreaker _fieldRockBreaker;
        private readonly IFieldRockEvents _fieldRockEvents;
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly IFieldStructureSceneBuilder _fieldStructureSceneBuilder;
        private readonly IFieldDebrisSceneBuilder _fieldDebrisSceneBuilder;
        private readonly List<FieldRockDestroyed> _destroyed = new List<FieldRockDestroyed>();

        public LoopSmokeRockBreakCheck() {
            _fieldRockBreakDebug = SessionQueries.Resolve<IFieldRockBreakDebug>();
            _fieldRockBreaker = SessionQueries.Resolve<IFieldRockBreaker>();
            _fieldRockEvents = SessionQueries.Resolve<IFieldRockEvents>();
            _liveFieldModel = SessionQueries.Resolve<IReadOnlyLiveFieldModel>();
            _fieldStructureSceneBuilder = SessionQueries.Resolve<IFieldStructureSceneBuilder>();
            _fieldDebrisSceneBuilder = SessionQueries.Resolve<IFieldDebrisSceneBuilder>();
        }

        [LoopSmokeStep(LoopSmokePhase.InCruise, 1, 1, 30)]
        public IEnumerator BreakNearestRockInCruiseCoroutine() {
            _fieldRockEvents.RockDestroyed += _destroyed.Add;
            FieldRockBreakResult result = _fieldRockBreakDebug.DebugBreakNearestRock(out FieldRockId id);
            FieldRockBreakResult again = _fieldRockBreaker.ServerBreak(id, FieldRockDestroyCause.ShipCollision, new PlayerKey(string.Empty));
            _fieldRockEvents.RockDestroyed -= _destroyed.Add;
            yield return null;

            Assert.AreEqual(FieldRockBreakResult.Broken, result, "The debug entry did not break a rock near the ship.");
            Assert.AreEqual(FieldRockBreakResult.AlreadyBroken, again, $"Rock {id} broke twice.");
            Assert.IsTrue(_liveFieldModel.BrokenRocks.Contains(id), $"Rock {id} is not in the synced broken list.");
            Assert.AreEqual(1, _destroyed.Count, "RockDestroyed must fire exactly once.");
            Assert.AreEqual(id, _destroyed[0].Id);
            Assert.IsTrue(TryGetBuiltRock(id, out FieldRockVisual rock), $"Rock {id} near the ship is not built on the host.");
            Assert.IsFalse(rock.gameObject.activeSelf, $"Broken rock {id} is still shown.");
            Debug.Log($"[LoopSmoke] broke rock {id}: {_destroyed[0].SourceId}, {_destroyed[0].Size}, {_destroyed[0].Diameter:0.0} m");
        }

        [LoopSmokeStep(LoopSmokePhase.AfterLanding, 1, 1, 10)]
        public void AssertBrokenListClearedAfterLanding() =>
            Assert.AreEqual(0, _liveFieldModel.BrokenRocks.Count, "The broken rocks of the field are still listed after landing.");

        private bool TryGetBuiltRock(FieldRockId id, out FieldRockVisual rock) =>
            id.Source == FieldRockSource.Cluster
                ? _fieldStructureSceneBuilder.TryGetBuiltRock(id.PlacementIndex, id.RockIndex, out rock)
                : _fieldDebrisSceneBuilder.TryGetBuiltRock(id.DebrisCell, id.RockIndex, out rock);
    }
}
#endif
