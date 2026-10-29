using System;
using Features.PointOfInterestModule.Scripts.Configuration;
using Features.PointOfInterestModule.Scripts.Network;
using Features.ShopModule.Scripts.Core;

namespace Features.PointOfInterestModule.Scripts.Server {
    public sealed class PointOfInterestOpenService : IPointOfInterestOpenService {
        private readonly IPointsOfInterest _pointsOfInterest;
        private readonly IPointOfInterestWriter _pointOfInterestWriter;
        private readonly IPointOfInterestContentCatalog _pointOfInterestContentCatalog;
        private readonly ICrewWalletBindingModel _crewWalletBindingModel;

        public PointOfInterestOpenService(IPointsOfInterest pointsOfInterest, IPointOfInterestWriter pointOfInterestWriter,
                                          IPointOfInterestContentCatalog pointOfInterestContentCatalog, ICrewWalletBindingModel crewWalletBindingModel) {
            _pointsOfInterest = pointsOfInterest;
            _pointOfInterestWriter = pointOfInterestWriter;
            _pointOfInterestContentCatalog = pointOfInterestContentCatalog;
            _crewWalletBindingModel = crewWalletBindingModel;
        }

        public PointOfInterestOpenResult ServerOpen(int pointId) {
            if (!_pointsOfInterest.TryFindIndex(pointId, out int index))
                return PointOfInterestOpenResult.NotFound;

            PointOfInterestEntry point = _pointsOfInterest.Get(index);
            if (point.State == PointOfInterestState.Opened)
                return PointOfInterestOpenResult.AlreadyOpened;

            point.State = PointOfInterestState.Opened;
            _pointOfInterestWriter.ServerSet(index, point);
            Pay(point);
            return PointOfInterestOpenResult.Opened;
        }

        // Points stand only in a flight of the game scene, where the wallet is bound.
        private void Pay(in PointOfInterestEntry point) {
            if (!_crewWalletBindingModel.IsBound)
                throw new InvalidOperationException($"Point of interest {point.Id} was opened with no crew wallet bound on the server.");

            _crewWalletBindingModel.Wallet.ServerEarn(_pointOfInterestContentCatalog.GetRequired(point.ContentId).Reward);
        }
    }
}
