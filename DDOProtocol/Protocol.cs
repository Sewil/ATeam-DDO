using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DDOProtocol
{
    public class Protocol
    {
        public Socket Socket { get; set; }
        public string Name { get; }
        public Encoding Encoding { get; }
        public int MsgSize { get; }
        public Protocol(string name, Encoding encoding, int msgSize, Socket socket = null)
        {
            Name = name;
            Encoding = encoding;
            MsgSize = msgSize;
            Socket = socket;
        }
        public Message Receive(int msgSizeOverride = 0)
        {
            if (msgSizeOverride == 0)
            {
                msgSizeOverride = MsgSize;
            }
            byte[] bufferIn = new byte[msgSizeOverride];
            Socket.Receive(bufferIn);
            string receivedMessage = Encoding.GetString(bufferIn).TrimEnd('\0');
            string[] attributes = receivedMessage.Split(' ');
            TransferMethod method = (TransferMethod)Enum.Parse(typeof(TransferMethod), attributes[0]);
            DataType dataType = (DataType)Enum.Parse(typeof(DataType), attributes[1]);
            string message = string.Empty;
            if (attributes.Length >= 5)
            {
                for (int i = 4; i < attributes.Length; i++)
                {
                    if (i > 4)
                    {
                        message += " ";
                    }
                    message += attributes[i];
                }
            }
            if (method == TransferMethod.Request)
            {
                RequestStatus status = (RequestStatus)Enum.Parse(typeof(RequestStatus), attributes[3]);
                return new Request(status, dataType, message);
            }
            else if (method == TransferMethod.Response)
            {
                ResponseStatus status = (ResponseStatus)Enum.Parse(typeof(ResponseStatus), attributes[3]);
                return new Response(status, dataType, message);
            }
            else
            {
                throw new ArgumentException("Couldn't receive invalid string.");
            }
        }
        public string GetMessage(Message transfer)
        {
            string message = string.Empty;
            if (transfer.Method == TransferMethod.Request)
            {
                message = $"{transfer.Method} {transfer.DataType} {Name} {(transfer as Request).Status} {transfer.Data}";
            }
            else
            {
                // response
                message = $"{transfer.Method} {transfer.DataType} {Name} {(transfer as Response).Status} {transfer.Data}";
            }
            return message;
        }
        public void Send(Message transfer)
        {
            Socket.Send(Encoding.GetBytes(GetMessage(transfer)));
        }
    }
}
