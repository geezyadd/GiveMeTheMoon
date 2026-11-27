using UnityEngine;
using Zenject;

namespace Features.BehaviourTreeModule {
    // Runs a tree on this object for a single peer: builds it in Start, ticks it in Update, halts it while disabled
    // and disposes it with the object. Unity cannot add a generic component, so every tree gets a one-line subclass.
    public abstract class BtTreeRunner<TTree> : MonoBehaviour, IBtTreeHolder where TTree : BtTree {
        private BtTreeFactory _treeFactory;
        private TTree _tree;
        private BtTickRateLimiter _tickRateLimiter;

        [Inject]
        private void InjectDependencies(DiContainer container) =>
            _treeFactory = new BtTreeFactory(container);

        private void Start() {
            _tree = _treeFactory.Create<TTree>();
            _tickRateLimiter = new BtTickRateLimiter(_tree.TicksPerSecond);
        }

        private void Update() {
            if (_tickRateLimiter.TryAdvance(Time.deltaTime, out float treeDeltaTime))
                _tree.Update(treeDeltaTime);
        }

        // OnDisable also comes before Start when the object starts disabled, so the tree may not exist yet.
        private void OnDisable() {
            if (_tree != null)
                _tree.Halt();
        }

        private void OnDestroy() {
            if (_tree != null)
                _tree.Dispose();
        }

        public bool TryGetTree(out BtTree tree) {
            tree = _tree;
            return _tree != null;
        }
    }
}
