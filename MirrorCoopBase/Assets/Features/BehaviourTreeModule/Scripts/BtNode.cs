using System;
using System.Collections.Generic;

namespace Features.BehaviourTreeModule {
    // One step of a tree. A node starts on its first tick (OnStart), runs every tick it is ticked (OnRun) and stops
    // when it finishes or is interrupted (OnStop runs in both cases, after its running children were stopped).
    // Services come through an [Inject] method (BtTreeFactory injects every node), plain values through the constructor.
    public abstract class BtNode {
        private const string CAPACITY_EXCEEDED = "{0} '{1}' takes at most {2} child node(s); '{3}' does not fit.";
        private const string ALREADY_ATTACHED = "Node '{0}' already belongs to a tree.";
        private const string INACTIVE_RESULT = "Node '{0}' returned Inactive from OnRun; return Running, Success or Failure.";

        private readonly List<BtNode> _children = new();

        public event Action<BtNode> OnStatusChanged;

        public string Name { get; }
        public BtStatus Status { get; private set; }
        public BtNode Parent { get; private set; }
        public BtTree Tree { get; private set; }
        public IReadOnlyList<BtNode> Children => _children;
        // The clock tick this node last ran on; the inspector dims nodes not ticked on the current tick.
        public int LastRunTick { get; private set; }

        // Shown by the editor inspector. Must have no side effects.
        public virtual string DebugText => string.Empty;

        // Whether starting this node now could do anything but fail at once ("would it fail now" is the negation).
        // A priority asks it before it interrupts a running branch for a higher one. Must have no side effects.
        public virtual bool CanEnter => true;

        // A pure check finishes in the tick it starts and has no side effects, so a sequence may re-check it every
        // tick while a later step runs.
        public virtual bool IsPureCheck => false;

        protected internal virtual int ChildCapacity => 0;

        protected BtClock Clock => Tree.Clock;

        protected BtNode(string name) =>
            Name = name;

        // A node that starts and finishes in the same tick with its previous result raises no status change, so
        // checks re-run every tick do not flood the inspector's log.
        public BtStatus Tick(float deltaTime) {
            BtStatus statusBefore = Status;
            if (statusBefore != BtStatus.Running) {
                OnStart();
                Status = BtStatus.Running;
            }

            LastRunTick = Tree.Clock.TickCount;
            BtStatus result = OnRun(deltaTime);
            if (result == BtStatus.Inactive)
                throw new InvalidOperationException(string.Format(INACTIVE_RESULT, Name));

            if (result != BtStatus.Running)
                Finish(result);

            if (Status != statusBefore)
                OnStatusChanged?.Invoke(this);

            return result;
        }

        public void Interrupt() {
            if (Status != BtStatus.Running)
                return;

            StopRunningChildren();
            OnStop();
            Status = BtStatus.Inactive;
            OnStatusChanged?.Invoke(this);
        }

        internal void AttachToTree(BtTree tree) {
            if (Tree != null)
                throw new InvalidOperationException(string.Format(ALREADY_ATTACHED, Name));

            Tree = tree;
        }

        internal void AddChild(BtNode child) {
            if (_children.Count >= ChildCapacity)
                throw new InvalidOperationException(string.Format(CAPACITY_EXCEEDED, GetType().Name, Name, ChildCapacity, child.Name));

            child.AttachToTree(Tree);
            child.Parent = this;
            _children.Add(child);
            OnChildAdded(child);
        }

        protected abstract BtStatus OnRun(float deltaTime);

        protected virtual void OnStart() { }

        protected virtual void OnStop() { }

        protected virtual void OnChildAdded(BtNode child) { }

        protected void StopRunningChildren() {
            for (int i = 0; i < _children.Count; i++)
                _children[i].Interrupt();
        }

        private void Finish(BtStatus result) {
            StopRunningChildren();
            OnStop();
            Status = result;
        }
    }
}
