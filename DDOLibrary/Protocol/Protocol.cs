using System;
using System.Net.Sockets;
using System.Text;

namespace DDOLibrary.Protocol {
    public class Protocol {
        const string NAME = "DDO";
        const double VERSION = 1.0;
        public static string Name {
            get {
                return $"{NAME}/{VERSION}";
            }
        }
        public Socket Socket { get; set; }

        public Encoding Encoding { get; }
        public int MsgSize { get; set; }
        public Protocol(Encoding encoding, int msgSize, Socket socket = null) {
            Encoding = encoding;
            MsgSize = msgSize;
            Socket = socket;
        }
        public Message Receive(int msgSizeOverride = 0) {
            try {
                if (msgSizeOverride == 0) {
                    msgSizeOverride = MsgSize;
                }
                byte[] bufferIn = new byte[msgSizeOverride];
                Socket.Receive(bufferIn);
                string receivedMessage = Encoding.GetString(bufferIn).TrimEnd('\0');
                string[] attributes = receivedMessage.Split(' ');

                TransferMethod method = (TransferMethod)Enum.Parse(typeof(TransferMethod), attributes[1]);
                DataType dataType = (DataType)Enum.Parse(typeof(DataType), attributes[3]);
                string message = string.Empty;
                if (attributes.Length >= 5) {
                    for (int i = 4; i < attributes.Length; i++) {
                        if (i > 4) {
                            message += " ";
                        }
                        message += attributes[i];
                    }
                }

                if (method == TransferMethod.REQUEST) {
                    RequestStatus status = (RequestStatus)Enum.Parse(typeof(RequestStatus), attributes[2]);
                    return new Request(status, dataType, message);
                } else if (method == TransferMethod.RESPONSE) {
                    ResponseStatus status = (ResponseStatus)Enum.Parse(typeof(ResponseStatus), attributes[2]);
                    return new Response(status, dataType, message);
                } else {
                    throw new ArgumentException("Couldn't receive invalid string.");
                }
            } catch {
                return null;
            }
        }
        public string GetMessage(Message transfer) {
            string message = string.Empty;
            if (transfer.Method == TransferMethod.REQUEST) {
                message = $"{Name} {transfer.Method} {(transfer as Request).Status} {transfer.DataType} {transfer.Data}";
            } else {
                message = $"{Name} {transfer.Method} {(transfer as Response).Status} {transfer.DataType} {transfer.Data}";
            }
            return message;
        }
        public void Send(Message transfer) {
            Socket.Send(Encoding.GetBytes(GetMessage(transfer)));
        }
    }
}
