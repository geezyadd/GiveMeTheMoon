namespace Features.BehaviourTreeModule {
    // What the editor inspector reads from a host: the tree, if this peer runs one.
    public interface IBtTreeHolder {
        public bool TryGetTree(out BtTree tree);
    }
}
