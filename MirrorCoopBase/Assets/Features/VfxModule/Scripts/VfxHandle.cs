namespace Features.VfxModule {
    // An attached effect, as its player holds it: stops it only while the instance still plays that same effect (a
    // reused instance has moved on to another generation).
    public readonly struct VfxHandle {
        private readonly VfxInstance _instance;
        private readonly int _generation;

        public VfxHandle(VfxInstance instance) {
            _instance = instance;
            _generation = instance.Generation;
        }

        public void Stop() {
            if (_instance != null && _instance.Generation == _generation)
                _instance.Stop();
        }
    }
}
