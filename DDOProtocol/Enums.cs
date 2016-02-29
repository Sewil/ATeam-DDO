namespace DDOProtocol {
    public enum ResponseType {
        SENDSTATE,
        MOVE_OK,
        MOVE_BLOCKED,
        LOGIN_ACCEPTED,
        LOGIN_REJECTED,
    }
    public enum RequestType {
        LOGIN,
        GETSTATE,
        MOVE
    }
}
