namespace Features.WeaponModule.Scripts.Hits {
    public enum WeaponHitVerdict {
        None = 0,
        Accepted = 1,
        WeaponNotHeld = 2,
        TargetNotAlive = 3,
        OutOfRange = 4,
        FireRateExceeded = 5,
        UnknownTarget = 6,
    }
}
