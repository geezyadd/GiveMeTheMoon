using System.Collections.Generic;
using Features.GameCoreModule.Contracts;

namespace Features.BugModule.Scripts.Editor.Tests {
    public sealed class RecordingShipHull : IShipHull {
        public List<ShipDamage> Damages { get; } = new();

        public void ServerApplyDamage(ShipDamage damage) =>
            Damages.Add(damage);
    }
}
