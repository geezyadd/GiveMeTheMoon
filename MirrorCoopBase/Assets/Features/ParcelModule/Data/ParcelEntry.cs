using System;

namespace Features.ParcelModule.Data {
    // One parcel the crew took, by its crate's net id: what it pays at which station, where it is, the field agent
    // that carries it (Carried) and the player whose rope pulls it with which weapon (Pulled).
    [Serializable]
    public struct ParcelEntry : IEquatable<ParcelEntry> {
        public uint NetId;
        public long Reward;
        public int DestinationLoop;
        public ParcelState State;
        public int CarrierAgentId;
        public string HookerId;
        public string HookWeaponId;

        public ParcelEntry(uint netId, long reward, int destinationLoop, ParcelState state, int carrierAgentId, string hookerId, string hookWeaponId) {
            NetId = netId;
            Reward = reward;
            DestinationLoop = destinationLoop;
            State = state;
            CarrierAgentId = carrierAgentId;
            HookerId = hookerId;
            HookWeaponId = hookWeaponId;
        }

        public ParcelEntry With(ParcelState state, int carrierAgentId, string hookerId, string hookWeaponId) =>
            new(NetId, Reward, DestinationLoop, state, carrierAgentId, hookerId, hookWeaponId);

        public bool Equals(ParcelEntry other) =>
            NetId == other.NetId && Reward == other.Reward && DestinationLoop == other.DestinationLoop && State == other.State
            && CarrierAgentId == other.CarrierAgentId && HookerId == other.HookerId && HookWeaponId == other.HookWeaponId;

        public override bool Equals(object obj) =>
            obj is ParcelEntry other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(NetId, Reward, DestinationLoop, State, CarrierAgentId, HookerId, HookWeaponId);
    }
}
