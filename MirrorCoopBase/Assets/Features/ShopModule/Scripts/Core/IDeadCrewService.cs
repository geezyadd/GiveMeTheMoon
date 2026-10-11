using System.Collections.Generic;
using Features.NetworkModelModule.Scripts;

namespace Features.ShopModule.Scripts.Core {
    public interface IDeadCrewService {
        public IReadOnlyList<PlayerKey> GetDeadOnlineCrew();
        public bool IsOnline(PlayerKey key);
        public bool IsDead(PlayerKey key);
    }
}
