using Zenject;

namespace Features.BehaviourTreeModule {
    // Creates a tree with its constructor dependencies, builds it and injects the services of every node.
    public sealed class BtTreeFactory {
        private readonly DiContainer _container;

        public BtTreeFactory(DiContainer container) =>
            _container = container;

        public TTree Create<TTree>() where TTree : BtTree {
            TTree tree = _container.Instantiate<TTree>();
            tree.Build();
            InjectBranch(tree.Root);
            return tree;
        }

        private void InjectBranch(BtNode node) {
            _container.Inject(node);
            for (int i = 0; i < node.Children.Count; i++)
                InjectBranch(node.Children[i]);
        }
    }
}
