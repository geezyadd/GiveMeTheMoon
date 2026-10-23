using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Network {
    // Marks the prefab of a networked field structure: a client finds the field's Mirror objects among everything
    // spawned by it, to move its copies with a world shift the way the station pads are moved.
    public sealed class FieldNetworkedStructure : MonoBehaviour {
    }
}
