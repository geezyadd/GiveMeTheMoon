using Features.FlightFieldModule.Contracts;
using Mirror;

namespace Features.FlightFieldModule.Scripts.Network {
    // Mirror's writer and reader for a target balloon id in the live field's popped list: the id is a read-only struct
    // of the contracts assembly, which does not reference Mirror.
    public static class FieldTargetIdSerializer {
        public static void WriteFieldTargetId(this NetworkWriter writer, FieldTargetId id) =>
            writer.WriteInt(id.Index);

        public static FieldTargetId ReadFieldTargetId(this NetworkReader reader) =>
            new(reader.ReadInt());
    }
}
