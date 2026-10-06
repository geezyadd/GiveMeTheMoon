using UnityEngine;

namespace Features.ShopModule.Scripts.Configurations {
    [CreateAssetMenu(
        fileName = nameof(ReviveConfiguration) + "_Default",
        menuName = "Configurations/ShopModule/" + nameof(ReviveConfiguration))]
    public sealed class ReviveConfiguration : ScriptableObject {
        [Tooltip("What the crew pays at a station shop to buy back a dead crewmate. Fixed in M1; a price that grows with every buy-back (M2) replaces this value.")]
        [SerializeField] private long _revivePrice = 300;

        public long RevivePrice =>
            _revivePrice;
    }
}
