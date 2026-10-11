using UnityEngine;

namespace Features.GrabModule.Scripts {
    // The server's check that a player is close enough to what they asked to grab or use.
    public sealed class InteractionReach {
        private readonly GrabConfiguration _configuration;

        public InteractionReach(GrabConfiguration configuration) =>
            _configuration = configuration;

        public bool Contains(Transform user, Vector3 target) {
            Vector3 origin = user.position + Vector3.up * _configuration.ServerReachOriginHeight;
            float reach = _configuration.AimRange + _configuration.ServerReachSlack;
            return (target - origin).sqrMagnitude <= reach * reach;
        }
    }
}
