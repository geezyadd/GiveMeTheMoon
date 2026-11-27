namespace Features.BehaviourTreeModule {
    // A node with any number of children; the builder closes it with End().
    public abstract class BtComposite : BtNode {
        protected BtComposite(string name) : base(name) { }

        protected internal override int ChildCapacity => int.MaxValue;
    }
}
