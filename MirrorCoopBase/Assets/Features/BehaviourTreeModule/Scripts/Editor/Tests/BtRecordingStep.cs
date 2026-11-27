using System.Collections.Generic;

namespace Features.BehaviourTreeModule.Editor.Tests {
    // A leaf that writes "start:", "run:" and "stop:" entries with its name into a shared journal and returns the
    // result the test sets.
    public sealed class BtRecordingStep : BtNode {
        public const string START = "start:";
        public const string RUN = "run:";
        public const string STOP = "stop:";

        private readonly List<string> _journal;

        public BtRecordingStep(string name, List<string> journal, BtStatus result) : base(name) {
            _journal = journal;
            Result = result;
        }

        public BtStatus Result { get; set; }

        protected override void OnStart() =>
            _journal.Add(START + Name);

        protected override BtStatus OnRun(float deltaTime) {
            _journal.Add(RUN + Name);
            return Result;
        }

        protected override void OnStop() =>
            _journal.Add(STOP + Name);
    }
}
