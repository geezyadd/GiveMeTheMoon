using System.Collections.Generic;
using Features.AttackModule;
using Features.CharacterMovableModule.Scripts;
using Features.PlayerReplicaModule;
using UnityEngine;
using Zenject;

namespace Features.PlayerBodyModule {
    // What a player's attack goes for (IAttackAim): the nearest head of another player in front, within the attack's
    // distance and angle from this player's head and its shown look (SlapAimRule.PickHead). A sleeping ragdoll (a dead
    // player, a body nobody simulates here) is not aimed at. Until the TargetModule exists, the heads are those of the
    // players' PlayerSlapHit.
    public sealed class PlayerAttackAim : MonoBehaviour, IAttackAim {
        [SerializeField] private CharacterMovableBase _movable;
        [Tooltip("This player's own slap receiver: its head is where the aim is measured from.")]
        [SerializeField] private PlayerSlapHit _ownHead;

        private readonly List<PlayerSlapHit> _candidates = new();
        private readonly List<Vector3> _candidateHeads = new();

        private BodyStateChannelRegistry _registry;

        // The player whose head the last pick went for; null when there was none. The head's transform alone does not
        // lead back to its player: an awake ragdoll's model is a scene root of its own.
        public PlayerSlapHit PickedHead { get; private set; }

        [Inject]
        private void Construct(BodyStateChannelRegistry registry) =>
            _registry = registry;

        public Transform PickTarget(AttackDefinition attack) {
            PickedHead = PickHead(attack);
            return PickedHead != null ? PickedHead.HeadCenter : null;
        }

        private PlayerSlapHit PickHead(AttackDefinition attack) {
            if (_registry == null || _ownHead.IsAwake == false)
                return null;

            _candidates.Clear();
            _candidateHeads.Clear();
            foreach (BodyStateChannel channel in _registry.Channels) {
                if (channel == null || channel.gameObject == gameObject)
                    continue;
                if (channel.TryGetComponent(out PlayerSlapHit other) == false || other.IsAwake == false)
                    continue;

                _candidates.Add(other);
                _candidateHeads.Add(other.HeadCenter.position);
            }

            int picked = SlapAimRule.PickHead(_ownHead.HeadCenter.position, _movable.ShownLookYaw, _candidateHeads,
                attack.MaxDistance, attack.MaxAngle);
            return picked >= 0 ? _candidates[picked] : null;
        }
    }
}
