using System;
using System.Collections.Generic;

namespace Features.BehaviourTreeModule.Nodes {
    // Runs the option with the highest positive score. The running option counts its score plus the stickiness, so
    // two close scores do not flip the choice every tick. An option that fails hands over to the next best one in
    // the same tick; the choice fails when no option is left.
    public sealed class BtScoredChoice : BtComposite {
        private const string NOT_A_SCORED_BRANCH = "Scored choice '{0}' takes only scored branches; '{1}' is a {2}.";
        private const string NEGATIVE_STICKINESS = "Stickiness cannot be negative.";
        private const int NONE = -1;

        private readonly float _stickiness;
        private readonly List<BtScoredBranch> _branches = new();
        private readonly List<bool> _failedThisTick = new();

        private int _activeIndex = NONE;

        public BtScoredChoice(string name, float stickiness) : base(name) {
            if (stickiness < 0f)
                throw new ArgumentOutOfRangeException(nameof(stickiness), stickiness, NEGATIVE_STICKINESS);

            _stickiness = stickiness;
        }

        public override bool CanEnter => FindBest() != NONE;

        protected override void OnStart() {
            _activeIndex = NONE;
            ClearFailures();
        }

        protected override void OnChildAdded(BtNode child) {
            if (child is not BtScoredBranch branch)
                throw new InvalidOperationException(string.Format(NOT_A_SCORED_BRANCH, Name, child.Name, child.GetType().Name));

            _branches.Add(branch);
            _failedThisTick.Add(false);
        }

        protected override BtStatus OnRun(float deltaTime) {
            ClearFailures();
            for (int best = FindBest(); best != NONE; best = FindBest()) {
                if (best != _activeIndex)
                    SwitchTo(best);

                BtStatus result = _branches[best].Tick(deltaTime);
                if (result != BtStatus.Failure)
                    return result;

                _failedThisTick[best] = true;
                _activeIndex = NONE;
            }

            return BtStatus.Failure;
        }

        private void SwitchTo(int index) {
            StopRunningChildren();
            _activeIndex = index;
        }

        private int FindBest() {
            int bestIndex = NONE;
            float bestScore = 0f;
            for (int i = 0; i < _branches.Count; i++) {
                if (!TryGetCandidateScore(i, out float score) || score <= bestScore)
                    continue;

                bestIndex = i;
                bestScore = score;
            }

            return bestIndex;
        }

        private bool TryGetCandidateScore(int index, out float score) {
            score = 0f;
            BtScoredBranch branch = _branches[index];
            if (_failedThisTick[index])
                return false;

            float rawScore = branch.Score;
            if (rawScore <= 0f)
                return false;

            bool isActive = index == _activeIndex && branch.Status == BtStatus.Running;
            if (!isActive && !branch.CanEnter)
                return false;

            score = isActive ? rawScore + _stickiness : rawScore;
            return true;
        }

        private void ClearFailures() {
            for (int i = 0; i < _failedThisTick.Count; i++)
                _failedThisTick[i] = false;
        }
    }
}
