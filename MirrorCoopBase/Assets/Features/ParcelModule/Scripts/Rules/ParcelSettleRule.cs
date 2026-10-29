using Features.ParcelModule.Data;

namespace Features.ParcelModule.Scripts.Rules {
    // At its destination a parcel lying on board (not under a thief, adrift or on a rope) is delivered, anything else
    // there is lost; a parcel for a later station waits, one for a station already passed is lost.
    public sealed class ParcelSettleRule : IParcelSettleRule {
        public ParcelSettlement Decide(in ParcelEntry entry, int stationLoop, bool isAboard) {
            if (entry.DestinationLoop > stationLoop)
                return ParcelSettlement.Keep;

            bool isDelivered = entry.DestinationLoop == stationLoop && entry.State == ParcelState.Loose && isAboard;
            return isDelivered ? ParcelSettlement.Deliver : ParcelSettlement.Lose;
        }
    }
}
