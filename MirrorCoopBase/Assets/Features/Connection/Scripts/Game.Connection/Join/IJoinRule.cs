namespace Game.Connection
{
    public interface IJoinRule
    {
        public ConnectionRejectReason Evaluate(JoinRequest request);
    }
}
