using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Shows how far physics lags behind the animation: white is the animated skeleton hung on the physical root
    // limb (where the limbs are pulled to), red goes from each physical limb to its target.
    [RequireComponent(typeof(RagdollBody))]
    public sealed class RagdollPoseGizmo : MonoBehaviour {
        [SerializeField] private bool _show = true;

        private void OnDrawGizmos() {
            if (_show == false || Application.isPlaying == false)
                return;

            RagdollBody body = GetComponent<RagdollBody>();
            RagdollLimb root = body.Root;
            if (root == null || root.HasTarget == false)
                return;

            Vector3 rootPosition = root.Rigidbody.position;
            foreach (RagdollLimb limb in body.Limbs) {
                Vector3 target = rootPosition + limb.Target.OffsetFromRoot;
                if (limb.Parent != null) {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(rootPosition + limb.Parent.Target.OffsetFromRoot, target);
                }

                Gizmos.color = Color.red;
                Gizmos.DrawLine(limb.Rigidbody.position, target);
            }
        }
    }
}
