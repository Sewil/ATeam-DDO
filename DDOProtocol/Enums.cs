namespace DDOProtocol
{
    public enum TransferMethod {
        Request,
        Response
    }
    public enum DataType {
        None,
        Text,
        Json,
        Xml
    }
    public enum ResponseStatus {
        None,
        OK,
        LimitReached,
        Unauthorized,
        OutOfBounds,
        NotFound,
        NotReady,
        BadRequest
    }
    public enum RequestStatus
    {
        None,
        GetAccountPlayers,
        SelectPlayer,
        GetState,
        WriteState,
        GetPlayer,
        Start,
        Login,
        Move,
        SendChatMessage
    }
}
