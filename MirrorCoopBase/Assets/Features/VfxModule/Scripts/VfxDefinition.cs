using Features.VfxModule.Effects;
using UnityEngine;

namespace Features.VfxModule {
    // One visual effect as data: its prefab and its pool. Whoever plays it references this asset; a new effect is a new
    // asset, never a new branch in code.
    [CreateAssetMenu(fileName = "VfxDefinition", menuName = "Vfx/Definition")]
    public sealed class VfxDefinition : ScriptableObject {
        [SerializeField] private VfxEffectBase _prefab;
        [Tooltip("Instances made the first time the effect plays, so the next ones do not instantiate.")]
        [SerializeField, Min(0)] private int _prewarmCount = 2;
        [Tooltip("At most this many at once; past it the oldest free-standing one is reused.")]
        [SerializeField, Min(1)] private int _maxCount = 10;
        [Tooltip("An instance plays at least this long, s, before it may go back to the pool.")]
        [SerializeField, Min(0f)] private float _minLifetime = 0.3f;
        [Tooltip("An instance goes back to the pool after this long, s, even if something of it is still alive.")]
        [SerializeField, Min(0.1f)] private float _maxLifetime = 8f;

        public VfxEffectBase Prefab => _prefab;
        public int PrewarmCount => _prewarmCount;
        public int MaxCount => _maxCount;
        public float MinLifetime => _minLifetime;
        public float MaxLifetime => _maxLifetime;
    }
}
