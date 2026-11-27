using System;

namespace Features.BehaviourTreeModule {
    // A tree is a plain class: a concrete tree takes its services and configs through the constructor (created with
    // DiContainer.Instantiate by BtTreeFactory) and describes its nodes in Describe with the builder.
    public abstract class BtTree : IDisposable {
        private const string NOT_BUILT = "Tree {0} has no root yet: build it before updating it.";
        private const string ALREADY_BUILT = "Tree {0} is already built.";
        private const string DISPOSED = "Tree {0} is disposed.";
        private const string NEGATIVE_DELTA = "A tree cannot go back in time.";

        private BtNode _root;
        private bool _isDisposed;

        public BtClock Clock { get; } = new();
        public BtNode Root => _root ?? throw new InvalidOperationException(string.Format(NOT_BUILT, GetType().Name));

        // 0 ticks the tree on every host update; a concrete tree overrides it with a value from its config.
        public virtual float TicksPerSecond => 0f;

        public void Build() {
            ThrowIfDisposed();
            if (_root != null)
                throw new InvalidOperationException(string.Format(ALREADY_BUILT, GetType().Name));

            _root = Describe(new BtBuilder(this));
        }

        public BtStatus Update(float deltaTime) {
            ThrowIfDisposed();
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, NEGATIVE_DELTA);

            BtNode root = Root;
            Clock.Advance(deltaTime);
            return root.Tick(deltaTime);
        }

        public void Halt() {
            ThrowIfDisposed();
            Root.Interrupt();
        }

        public void Dispose() {
            if (_isDisposed)
                return;

            if (_root != null)
                _root.Interrupt();

            _isDisposed = true;
        }

        protected abstract BtNode Describe(BtBuilder builder);

        private void ThrowIfDisposed() {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name, string.Format(DISPOSED, GetType().Name));
        }
    }
}
