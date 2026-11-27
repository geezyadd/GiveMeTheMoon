using System;

namespace Features.BehaviourTreeModule.Editor.Tests {
    public sealed class BtTestTree : BtTree {
        private readonly Func<BtBuilder, BtNode> _describe;

        public BtTestTree(Func<BtBuilder, BtNode> describe) =>
            _describe = describe;

        public static BtTestTree CreateBuilt(Func<BtBuilder, BtNode> describe) {
            BtTestTree tree = new(describe);
            tree.Build();
            return tree;
        }

        protected override BtNode Describe(BtBuilder builder) =>
            _describe(builder);
    }
}
