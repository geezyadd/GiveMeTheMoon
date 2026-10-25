using System;

namespace Features.DebugModule.Contracts {
    // Editor / development builds only: a module registers its debug actions here (where it used to read a debug
    // key) and disposes the returned handle when it goes away; the debug window is built from what is registered.
    public interface IDebugActionRegistry {
        public IDisposable Register(DebugActionDefinition action);
    }
}
