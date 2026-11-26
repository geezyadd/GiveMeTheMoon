using Features.VfxModule.Effects;
using UnityEngine;

namespace Features.VfxModule {
    public interface IVfxFactory {
        public VfxEffectBase Create(VfxEffectBase prefab, Transform parent);
    }
}
