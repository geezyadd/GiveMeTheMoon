using Features.BugModule.Scripts.Configuration;
using Features.EffectPoolModule.Scripts;
using UnityEngine;

namespace Features.BugModule.Scripts.View {
    // Bug deaths played from a pool of the configured effect at the scene root: an effect is over in about a second,
    // far quicker than a world shift matters.
    public sealed class BugDeathEffectPool : IBugDeathEffectPool {
        private readonly SelfDisablingEffectPool<BugDeathEffect> _pool = new();
        private readonly BugConfiguration _bugConfiguration;

        public BugDeathEffectPool(BugConfiguration bugConfiguration) =>
            _bugConfiguration = bugConfiguration;

        public void Play(Vector3 position) =>
            _pool.TakeFree(_bugConfiguration.DeathEffectPrefab, null).Play(position);
    }
}
