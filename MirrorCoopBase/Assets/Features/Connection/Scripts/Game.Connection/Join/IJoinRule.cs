namespace Game.Connection
{
    public interface IJoinRule
    {
        public ConnectionRejectReason Evaluate(JoinRequest request);

        // The session lets players in at this moment, whether or not a slot is free.
        public bool IsJoinWindowOpen(JoinRequest request);
    }
}
