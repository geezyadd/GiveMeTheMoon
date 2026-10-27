#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using Features.CharacterMovableModule.Scripts;
using Features.FloatingControllerModule;
using Features.PlayerLifeModule.Scripts;
using Features.ShipModule.Scripts;
using Mirror;
using UnityEngine;

namespace Tests.Shared {
    // Whether the local player is free of the ship (after a run ended in flight): not riding, not seated, its body
    // under physics with collisions, hover and its network body, alive and with unblocked controls, and how far it
    // walks under the move input the keyboard fills. Used by the coop host test and the coop client bot (no NUnit there).
    public static class PlayerReleaseProbe {
        private const string SEPARATOR = "; ";
        private const float WALK_SECONDS = 0.4f;
        private static readonly WaitForSeconds _walk = new WaitForSeconds(WALK_SECONDS);

        // Empty when the player is free.
        public static string DescribeProblems(NetworkIdentity player) {
            List<string> problems = new List<string>();
            ShipRider rider = player.GetComponent<ShipRider>();
            Rigidbody body = player.GetComponent<Rigidbody>();
            AddIf(problems, rider.IsRiding, "still rides a ship");
            AddIf(problems, rider.IsSeated, "still seated");
            AddIf(problems, body.isKinematic, "body kinematic");
            AddIf(problems, body.detectCollisions == false, "collisions off");
            AddIf(problems, player.GetComponent<FloatingController>().HoverEnabled == false, "hover off");
            AddIf(problems, player.GetComponent<NetworkRigidbodyUnreliable>().enabled == false, "network body silenced");
            AddIf(problems, player.GetComponent<PlayerLifeBody>().Damageable.IsDead, "dead");
            AddIf(problems, SessionQueries.Resolve<CharacterInputBuffer>().IsBlocked, "controls blocked");
            return string.Join(SEPARATOR, problems);
        }

        // Through the same input buffer the keyboard fills; the distance walked on the ground plane.
        public static IEnumerator WalkCoroutine(NetworkIdentity player, Vector2 move, System.Action<float> walked) {
            CharacterInputBuffer input = SessionQueries.Resolve<CharacterInputBuffer>();
            Vector3 start = player.transform.position;
            input.BeginScripted();
            input.SetScripted(move, false, false);
            yield return _walk;
            input.EndScripted();
            Vector3 delta = player.transform.position - start;
            delta.y = 0f;
            walked(delta.magnitude);
        }

        private static void AddIf(List<string> problems, bool isProblem, string problem) {
            if (isProblem)
                problems.Add(problem);
        }
    }
}
#endif
