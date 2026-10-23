namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // How a shot reaches its target. The outcome may arrive later than the call: nothing assumes the shot is instant.
    public interface IShotDelivery {
        public void Deliver(in WeaponShot shot, IShotOutcomeReceiver receiver);
    }
}
