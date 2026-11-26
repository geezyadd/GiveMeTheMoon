namespace Features.AttackModule {
    // Turns an attack counter into presses, pure. A press is the counter changing between two reads while the presses
    // are available (once for any number of presses missed in between); the first count read after they were not
    // available is taken as it is, so a copy that starts or a body that wakes does not attack on its own.
    public sealed class AttackPressCounter {
        private bool _hasCount;
        private byte _count;

        public bool Step(bool isAvailable, byte count) {
            if (isAvailable == false) {
                _hasCount = false;
                return false;
            }

            if (_hasCount == false) {
                _hasCount = true;
                _count = count;
                return false;
            }

            bool isPressed = count != _count;
            _count = count;
            return isPressed;
        }

        public void Reset() =>
            _hasCount = false;
    }
}
