namespace Features.BehaviourTreeModule.Editor {
    public readonly struct BtStatusChange {
        public BtStatusChange(double treeSeconds, string nodeName, BtStatus status) {
            TreeSeconds = treeSeconds;
            NodeName = nodeName;
            Status = status;
        }

        public double TreeSeconds { get; }
        public string NodeName { get; }
        public BtStatus Status { get; }
    }
}
