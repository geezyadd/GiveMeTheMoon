using System;
using Features.BugModule.Scripts.Network;
using Features.FieldAgentModule.Scripts.Client;
using Zenject;

namespace Features.BugModule.Scripts.View {
    // Every peer: a bug the server killed bursts where this peer shows it, and its look vanishes until the next agent
    // batch leaves it out. A bug this peer does not show has nothing to play.
    public sealed class BugDeathViewSystem : IInitializable, IDisposable {
        private readonly IBugDeathEvents _bugDeathEvents;
        private readonly IFieldAgentShownAgents _fieldAgentShownAgents;
        private readonly IBugDeathEffectPool _bugDeathEffectPool;

        public BugDeathViewSystem(IBugDeathEvents bugDeathEvents, IFieldAgentShownAgents fieldAgentShownAgents, IBugDeathEffectPool bugDeathEffectPool) {
            _bugDeathEvents = bugDeathEvents;
            _fieldAgentShownAgents = fieldAgentShownAgents;
            _bugDeathEffectPool = bugDeathEffectPool;
        }

        public void Initialize() =>
            _bugDeathEvents.OnBugDied += HandleBugDied;

        public void Dispose() =>
            _bugDeathEvents.OnBugDied -= HandleBugDied;

        private void HandleBugDied(int bugId) {
            if (_fieldAgentShownAgents.TryGetView(bugId, out FieldAgentView view) == false || view.TryGetComponent(out BugBodyView bodyView) == false)
                return;

            _bugDeathEffectPool.Play(view.transform.position);
            bodyView.PresentDeath();
        }
    }
}
