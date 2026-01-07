namespace IRMClient.State
{
    public class UserInfo
    {
        public bool IsReady { get; set; } = false;
        public int Id { get; set; } = -1;
        public bool IsMaster { get; set; }
    }
}