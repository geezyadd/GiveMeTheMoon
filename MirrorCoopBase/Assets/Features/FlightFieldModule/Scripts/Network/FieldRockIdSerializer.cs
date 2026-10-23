using Features.FlightFieldModule.Contracts;
using Mirror;

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

        // The arguments are read left to right, in the order they were written.
        public static FieldRockId ReadFieldRockId(this NetworkReader reader) =>
            new((FieldRockSource)reader.ReadInt(), reader.ReadInt(), reader.ReadVector2Int(), reader.ReadInt());
    }
}
