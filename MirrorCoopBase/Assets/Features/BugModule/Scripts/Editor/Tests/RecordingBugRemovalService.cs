using System.Collections.Generic;
using Features.BugModule.Scripts.Brain;
using Features.BugModule.Scripts.Health;

namespace Features.BugModule.Scripts.Editor.Tests {
    // Records the kills and takes the bug out of the model, as the real removal does, without a field or a network.
    public sealed class RecordingBugRemovalService : IBugRemovalService {
        private readonly BugServerModel _bugServerModel;

        public List<int> Killed { get; } = new();
        public List<int> Gone { get; } = new();

        public RecordingBugRemovalService(BugServerModel bugServerModel) =>
            _bugServerModel = bugServerModel;

        public void ServerKill(int bugId) {
            Killed.Add(bugId);
            _bugServerModel.Remove(bugId);
        }

        public void ServerRemoveGone(int bugId) {
            Gone.Add(bugId);
            _bugServerModel.Remove(bugId);
        }

        public void ServerForgetVanished(int bugId) =>
            _bugServerModel.Remove(bugId);

        public void ServerForgetAll() =>
            _bugServerModel.Clear();
    }
}
