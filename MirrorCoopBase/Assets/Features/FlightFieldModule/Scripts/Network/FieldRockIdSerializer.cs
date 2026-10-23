using Features.FlightFieldModule.Contracts;
using Mirror;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Network {
    // Mirror's writer and reader for a rock id in the live field's broken list: the id is a read-only struct of the
    // contracts assembly, which does not reference Mirror.
    public static class FieldRockIdSerializer {
        public static void WriteFieldRockId(this NetworkWriter writer, FieldRockId id) {
            writer.WriteInt((int)id.Source);
            writer.WriteInt(id.PlacementIndex);
            writer.WriteVector2Int(id.DebrisCell);
            writer.WriteInt(id.RockIndex);
        }

        public static FieldRockId ReadFieldRockId(this NetworkReader reader) {
            FieldRockSource source = (FieldRockSource)reader.ReadInt();
            int placementIndex = reader.ReadInt();
            Vector2Int debrisCell = reader.ReadVector2Int();
            int rockIndex = reader.ReadInt();
            return new FieldRockId(source, placementIndex, debrisCell, rockIndex);
        }
    }
}
