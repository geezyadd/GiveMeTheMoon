using Mirror;
using UnityEngine;

namespace Features.ShopModule.Scripts.Core {
    public interface IShopBuyer {
        public NetworkIdentity Identity { get; }
        public Vector3 Position { get; }
        public bool IsAlive { get; }
    }
}
