using Features.FlightFieldModule.Contracts;
using UnityEngine;

namespace Features.SonarModule.Scripts.Sources {
    public interface IRockSonarContactIds {
        public string CreateRockId(FieldRockId rockId);
        public string CreateDebrisId(Vector2Int cell);
        public bool TryParseRockId(string contactId, out FieldRockId rockId);
        public bool TryParseDebrisId(string contactId, out Vector2Int cell);
    }
}
