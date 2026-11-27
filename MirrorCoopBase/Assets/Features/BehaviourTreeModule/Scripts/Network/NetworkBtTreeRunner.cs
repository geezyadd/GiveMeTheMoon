using Mirror;
using UnityEngine;
using Zenject;

namespace Features.BehaviourTreeModule.Network {
    // Runs a tree on the server only: it is built in OnStartServer and disposed in OnStopServer; clients never have
    // one and nothing of it is synced (what the tree commands is synced by the components it drives).
    // Unity cannot add a generic component, so every tree gets a one-line subclass.
    public abstract class NetworkBtTreeRunner<TTree> : NetworkBehaviour, IBtTreeHolder where TTree : BtTree {
        private BtTreeFactory _treeFactory;
        private TTree _tree;
        private BtTickRateLimiter _tickRateLimiter;

        [Inject]
        private void InjectDependencies(DiContainer container) =>
            _treeFactory = new BtTreeFactory(container);

        private void Update() {
            // Clients and a server that stopped have no tree.
            if (_tree == null)
                return;

            if (_tickRateLimiter.TryAdvance(Time.deltaTime, out float treeDeltaTime))
                _tree.Update(treeDeltaTime);
        }

        private void OnDisable() {
            if (_tree != null)
                _tree.Halt();
        }

        public override void OnStartServer() {
            _tree = _treeFactory.Create<TTree>();
            _tickRateLimiter = new BtTickRateLimiter(_tree.TicksPerSecond);
        }

        public override void OnStopServer() {
            _tree.Dispose();
            _tree = null;
            _tickRateLimiter = null;
        }

        public bool TryGetTree(out BtTree tree) {
            tree = _tree;
            return _tree != null;
        }
    }
}
