namespace DDOLibrary.Protocol
{
    public abstract class Message
    {
        public string Data { get; }
        public DataType DataType { get; }
        public TransferMethod Method { get; protected set; }
        public Message(DataType dataType = DataType.NONE, string data = null)
        {
            DataType = dataType;
            Data = data;
        }
    }
}
