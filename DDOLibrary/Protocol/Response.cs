namespace DDOLibrary.Protocol {
    public class Response : Message
    {
        public ResponseStatus Status { get; }
        public Response(ResponseStatus status, DataType dataType = DataType.NONE, string data = null) : base(dataType, data)
        {
            Method = TransferMethod.RESPONSE;
            Status = status;
        }
    }
}
