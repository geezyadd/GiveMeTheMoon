using UnityEngine;

namespace Features.BugModule.Scripts.Gnaw {
    public interface IBugGnawService {
        // Server: one bite of a bug sitting at the world point: hull HP, and maybe a broken module or a breach.
        public void ServerBite(int bugId, Vector3 worldPoint);
    }
}
