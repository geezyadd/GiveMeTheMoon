namespace Features.ShopModule.Scripts.Core {
    // What the server knows about one buy-back of a dead crewmate.
    public readonly struct ReviveRequest {
        public ReviveRequest(ShopAccessStatus buyerAccess, bool isTargetOnline, bool isTargetDead, long balance, long price) {
            BuyerAccess = buyerAccess;
            IsTargetOnline = isTargetOnline;
            IsTargetDead = isTargetDead;
            Balance = balance;
            Price = price;
        }

        // The same kiosk check as a purchase: Build phase, buyer alive, buyer close to the kiosk.
        public ShopAccessStatus BuyerAccess { get; }
        public bool IsTargetOnline { get; }
        public bool IsTargetDead { get; }
        public long Balance { get; }
        public long Price { get; }
    }
}
