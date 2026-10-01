using UnityEngine;
using UnityEngine.UI;

namespace Features.ShopModule.Scripts.UI {
    public sealed class BalanceView : BalanceViewBase {
        private const string BALANCE_FORMAT = "Balance: {0:N0}";

        [SerializeField] private Text _label;

        public override void SetBalance(long balance) =>
            _label.text = string.Format(BALANCE_FORMAT, balance);
    }
}
