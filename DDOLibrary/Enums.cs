namespace DDOLibrary {
    public enum Action {
        Add, Remove, Update
    }
    public enum CellType {
        Forest, Ground
    }
    public enum Direction {
        Up, Right, Down, Left
    }
    public enum TransferMethod {
        REQUEST,
        RESPONSE
    }
    public enum DataType {
        NONE,
        TEXT,
        JSON
    }
    public enum ResponseStatus {
        NONE,
        OK,
        LIMIT_REACHED,
        UNAUTHORIZED,
        OUT_OF_BOUNDS,
        NOT_FOUND,
        NOT_READY,
        BAD_REQUEST
    }
    public enum RequestStatus {
        NONE,
        UPDATE_STATE,
        START,
        LOGIN,
        MOVE,
        SEND_CHAT_MESSAGE
    }
}