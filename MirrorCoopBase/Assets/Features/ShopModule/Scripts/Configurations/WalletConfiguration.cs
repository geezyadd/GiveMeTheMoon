using UnityEngine;

namespace Features.ShopModule.Scripts.Configurations {
    [CreateAssetMenu(
        fileName = nameof(WalletConfiguration) + "_Default",
        menuName = "Configurations/ShopModule/" + nameof(WalletConfiguration))]
    public sealed class WalletConfiguration : ScriptableObject {
        [SerializeField] private long _startingBalance = 300;
        [Tooltip("How long the HUD balance flashes after the crew earned money, seconds.")]
        [SerializeField, Min(0.01f)] private float _incomeFlashSeconds = 0.6f;
        [Tooltip("The colour the HUD balance flashes in when the crew earned money; it fades back to the label's own colour.")]
        [SerializeField] private Color _incomeFlashColor = new(1f, 0.85f, 0.2f);
        [Tooltip("How much the HUD balance grows at the start of the flash (1 = not at all).")]
        [SerializeField, Min(1f)] private float _incomeFlashScale = 1.25f;
        [Tooltip("How much the debug window's Shop money action adds to the crew wallet (editor and development builds only).")]
        [SerializeField, Min(1)] private long _debugGrantAmount = 1000;

        public long StartingBalance => _startingBalance;
        public float IncomeFlashSeconds => _incomeFlashSeconds;
        public Color IncomeFlashColor => _incomeFlashColor;
        public float IncomeFlashScale => _incomeFlashScale;
        public long DebugGrantAmount => _debugGrantAmount;
    }
}
