using UnityEngine;

namespace Features.ShopModule.Scripts.Configurations {
    [CreateAssetMenu(
        fileName = nameof(WalletConfiguration) + "_Default",
        menuName = "Configurations/ShopModule/" + nameof(WalletConfiguration))]
    public sealed class WalletConfiguration : ScriptableObject {
        [SerializeField] private long _startingBalance = 1000000;

        public long StartingBalance => _startingBalance;
    }
}
