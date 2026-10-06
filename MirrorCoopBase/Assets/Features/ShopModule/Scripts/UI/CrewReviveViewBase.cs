using System;
using System.Collections.Generic;
using Features.MvpModule;
using Features.NetworkModelModule.Scripts;

namespace Features.ShopModule.Scripts.UI {
    public abstract class CrewReviveViewBase : ViewBehaviour {
        public event Action<PlayerKey> OnReviveClicked;

        public abstract void SetCrew(IReadOnlyList<CrewReviveDisplay> crew);

        protected void InvokeReviveClicked(PlayerKey player) =>
            OnReviveClicked?.Invoke(player);
    }
}
