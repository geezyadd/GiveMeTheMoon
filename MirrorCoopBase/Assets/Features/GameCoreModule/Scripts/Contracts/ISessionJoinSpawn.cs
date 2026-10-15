using UnityEngine;

namespace Features.GameCoreModule.Contracts {
    // Where a player who joins a running map appears; false when the map has no such place (the lobby).
    public interface ISessionJoinSpawn {
        public bool TryTakeJoinPose(out Pose pose);
    }
}
