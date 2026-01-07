namespace IRMShared
{
    public enum EMessageType : byte
    {
        NONE,
        REQ_USER_INFO,
        RES_USER_INFO,
        CLIENT_STATE_UPDATED,
        ALL_CLIENTS_INFO
    }
}