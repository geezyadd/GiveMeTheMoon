using System;

namespace Features.ParcelModule.Data {
    // One parcel the station's delivery stand offers: its place on the stand, what it pays, the loop of the station
    // it goes to (the next one) and whether the crew took it already.
    [Serializable]
    public struct ParcelOffer : IEquatable<ParcelOffer> {
        public int Slot;
        public long Reward;
        public int DestinationLoop;
        public bool IsTaken;

        public ParcelOffer(int slot, long reward, int destinationLoop, bool isTaken) {
            Slot = slot;
            Reward = reward;
            DestinationLoop = destinationLoop;
            IsTaken = isTaken;
        }

        public bool Equals(ParcelOffer other) =>
            Slot == other.Slot && Reward == other.Reward && DestinationLoop == other.DestinationLoop && IsTaken == other.IsTaken;

        public override bool Equals(object obj) =>
            obj is ParcelOffer other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Slot, Reward, DestinationLoop, IsTaken);
    }
}
