using System;
using System.Globalization;

namespace Features.BehaviourTreeModule.Nodes {
    // One option of a scored choice: its child plus the score the choice compares. The score must have no side effects.
    public sealed class BtScoredBranch : BtDecorator {
        private const string SCORE_FORMAT = "score {0:0.00}";

        private readonly Func<float> _score;

        public BtScoredBranch(string name, Func<float> score) : base(name) =>
            _score = score;

        public float Score => _score();
        public override bool CanEnter => Child.CanEnter;

        public override string DebugText =>
            string.Format(CultureInfo.InvariantCulture, SCORE_FORMAT, Score);

        protected override BtStatus OnRun(float deltaTime) =>
            Child.Tick(deltaTime);
    }
}
