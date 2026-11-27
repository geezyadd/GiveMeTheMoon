namespace Features.BehaviourTreeModule {
    // A node with exactly one child; the builder closes it by itself once the child is added.
    public abstract class BtDecorator : BtNode {
        protected BtDecorator(string name) : base(name) { }

        protected internal override int ChildCapacity => 1;

        protected BtNode Child => Children[0];
    }
}
