namespace Game.Connection
{
    public interface IConnectionRejectMessages
    {
        public string Describe(ConnectionRejectReason reason);
    }
}
