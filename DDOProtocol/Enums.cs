﻿namespace DDOProtocol
{
    public enum TransferMethod {
        REQUEST,
        RESPONSE
    }
    public enum DataType {
        TEXT,
        JSON,
        XML
    }
    public enum ResponseStatus {
        OK,
        UNAUTHORIZED,
        MOVE_BLOCKED,
        NOT_FOUND,
        NOT_READY,
        BAD_REQUEST
    }
    public enum RequestStatus
    {
        NONE,
        GET_ACCOUNT_PLAYERS,
        SELECT_PLAYER,
        GET_MAP,
        GET_PLAYER,
        START,
        LOGIN,
        GETSTATE,
        MOVE,
        SENDSTATE,
    }
}
