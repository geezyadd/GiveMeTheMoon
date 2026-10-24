using Features.MvpModule;
using UnityEngine;

namespace Features.ShopModule.Scripts.UI {
    public abstract class BalanceViewBase : ViewBehaviour {
        public abstract void SetBalance(long balance);
        public abstract void PlayIncomeFlash(float seconds, Color color, float scale);
    }
}
