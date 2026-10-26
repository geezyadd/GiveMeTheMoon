namespace Features.ShipImpactModule.Scripts {
    public interface IShipRockDamageRule {
        public float CalculateSmallRockDamage(float rockDiameter, float shipSpeed);
        public float CalculateLargeRockDamage();
        // 0..1: how hard a hit at this speed is felt, 1 at the reference speed and above.
        public float CalculateStrength(float shipSpeed);
    }
}
