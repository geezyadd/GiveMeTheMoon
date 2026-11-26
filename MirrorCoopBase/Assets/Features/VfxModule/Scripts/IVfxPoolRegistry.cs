using System.Collections.Generic;

namespace Features.VfxModule {
    // The pool of every effect played so far, made the first time an effect plays.
    public interface IVfxPoolRegistry {
        public IReadOnlyCollection<VfxPool> All { get; }

        public VfxPool Get(VfxDefinition effect);
    }
}
