using Features.NetworkModelModule.Scripts;

namespace Features.ShopModule.Scripts.UI {
    public readonly struct CrewReviveDisplay {
        public CrewReviveDisplay(PlayerKey player, string name, string price, bool canRevive) {
            Player = player;
            Name = name;
            Price = price;
            CanRevive = canRevive;
        }

        public PlayerKey Player { get; }
        public string Name { get; }
        public string Price { get; }
        public bool CanRevive { get; }
    }
}
