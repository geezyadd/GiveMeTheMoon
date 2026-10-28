using Features.BugModule.Scripts.Health;
using Features.FieldAgentModule.Scripts.Client;
using Features.WeaponModule.Contracts;
using UnityEngine;
using Zenject;

namespace Features.BugModule.Scripts.View {
    // The trigger a weapon ray strikes on a bug's look (layer FieldRock, as every weapon target) and the bug's weapon
    // target id, on every peer; a strike makes the bug flinch. The server never reads it: a reported hit is checked
    // against the bug model.
    public sealed class BugTargetCollider : MonoBehaviour, IWeaponTargetCollider, IWeaponImpactFeedback, IFieldAgentViewLifetime {
        [SerializeField] private Collider _collider;
        [SerializeField] private BugBodyView _bodyView;

        private IBugWeaponIds _bugWeaponIds;

        public ulong TargetId { get; private set; }

        [Inject]
        private void InjectDependencies(IBugWeaponIds bugWeaponIds) =>
            _bugWeaponIds = bugWeaponIds;

        public void Begin(int agentId) {
            TargetId = _bugWeaponIds.GetTargetId(agentId);
            SetStrikable(true);
        }

        public void End() {
            TargetId = 0;
            SetStrikable(false);
        }

        public void PresentImpact(Vector3 point, Vector3 normal) =>
            _bodyView.PresentStrike();

        public void SetStrikable(bool isStrikable) =>
            _collider.enabled = isStrikable;
    }
}
