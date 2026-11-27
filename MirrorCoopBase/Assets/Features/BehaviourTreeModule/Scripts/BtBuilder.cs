using System;
using System.Collections.Generic;
using Features.BehaviourTreeModule.Nodes;

namespace Features.BehaviourTreeModule {
    // Describes a tree top-down: a composite stays open for children until End(), a decorator wraps the next node
    // and closes by itself, a leaf is added where the builder stands.
    public sealed class BtBuilder {
        private const string SECOND_ROOT = "The tree already has a closed root; '{0}' has no parent to go into.";
        private const string NOTHING_TO_END = "End() has no open composite to close.";
        private const string DECORATOR_WITHOUT_CHILD = "End() reached decorator '{0}' before its child was added.";
        private const string EMPTY_COMPOSITE = "Composite '{0}' is closed without children.";
        private const string NO_ROOT = "The tree has no nodes.";
        private const string NOT_CLOSED = "Node '{0}' is still open: close every composite with End() before Build().";

        private readonly BtTree _tree;
        private readonly Stack<BtNode> _openNodes = new();

        private BtNode _root;

        internal BtBuilder(BtTree tree) =>
            _tree = tree;

        public BtBuilder Priority(string name) =>
            Add(new BtPriority(name));

        public BtBuilder Sequence(string name) =>
            Add(new BtSequence(name));

        public BtBuilder ScoredChoice(string name, float stickiness) =>
            Add(new BtScoredChoice(name, stickiness));

        public BtBuilder Scored(string name, Func<float> score) =>
            Add(new BtScoredBranch(name, score));

        public BtBuilder Guard(string name, Func<bool> holds) =>
            Add(new BtGuard(name, holds));

        public BtBuilder Not(string name) =>
            Add(new BtNot(name));

        public BtBuilder Recharge(string name, float seconds) =>
            Add(new BtRecharge(name, seconds));

        public BtBuilder Check(string name, Func<bool> holds) =>
            Add(new BtCheck(name, holds));

        public BtBuilder Delay(string name, float seconds) =>
            Add(new BtDelay(name, seconds));

        public BtBuilder Do(string name, Func<float, BtStatus> run) =>
            Add(new BtAction(name, run));

        public BtBuilder Add(BtNode node) {
            AttachToOpenNode(node);
            if (node.ChildCapacity > 0)
                _openNodes.Push(node);
            else
                CloseFilledDecorators();

            return this;
        }

        public BtBuilder End() {
            if (_openNodes.Count == 0)
                throw new InvalidOperationException(NOTHING_TO_END);

            BtNode node = _openNodes.Peek();
            if (node.ChildCapacity == 1)
                throw new InvalidOperationException(string.Format(DECORATOR_WITHOUT_CHILD, node.Name));

            if (node.Children.Count == 0)
                throw new InvalidOperationException(string.Format(EMPTY_COMPOSITE, node.Name));

            _openNodes.Pop();
            CloseFilledDecorators();
            return this;
        }

        public BtNode Build() {
            if (_root == null)
                throw new InvalidOperationException(NO_ROOT);

            if (_openNodes.Count > 0)
                throw new InvalidOperationException(string.Format(NOT_CLOSED, _openNodes.Peek().Name));

            return _root;
        }

        private void AttachToOpenNode(BtNode node) {
            if (_root == null) {
                node.AttachToTree(_tree);
                _root = node;
                return;
            }

            if (_openNodes.Count == 0)
                throw new InvalidOperationException(string.Format(SECOND_ROOT, node.Name));

            _openNodes.Peek().AddChild(node);
        }

        private void CloseFilledDecorators() {
            while (_openNodes.Count > 0 && IsFilled(_openNodes.Peek()))
                _openNodes.Pop();
        }

        private static bool IsFilled(BtNode node) =>
            node.Children.Count >= node.ChildCapacity;
    }
}
