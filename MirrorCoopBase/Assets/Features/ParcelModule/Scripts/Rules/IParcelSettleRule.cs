using Features.ParcelModule.Data;

namespace Features.ParcelModule.Scripts.Rules {
    public interface IParcelSettleRule {
        public ParcelSettlement Decide(in ParcelEntry entry, int stationLoop, bool isAboard);
    }
}
