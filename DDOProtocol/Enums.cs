namespace DDOProtocol
{
    public enum TransferMethod {
        REQUEST,
        RESPONSE
    }
    public enum DataType {
        NONE,
        TEXT,
        JSON,
        XML
    }
    public enum ResponseStatus {
        OK,
        LIMIT_REACHED,
        UNAUTHORIZED,
        OUT_OF_BOUNDS,
        NOT_FOUND,
        NOT_READY,
        BAD_REQUEST
    }
    public enum RequestStatus
    {
        NONE,
        GET_ACCOUNT_PLAYERS,
        SELECT_PLAYER,
        GET_STATE,
        WRITE_STATE,
        GET_PLAYER,
        START,
        LOGIN,
        MOVE,
        SEND_CHAT_MESSAGE
    }
}
