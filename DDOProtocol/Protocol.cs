using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DDOProtocol {
    public class Protocol {
        public Socket Socket { get; set; }
        public string Name { get; }
        public Encoding Encoding { get; }
        public int MsgSize { get; }
        public Protocol(string name, Encoding encoding, int msgSize, Socket socket = null) {
            Name = name;
            Encoding = encoding;
            MsgSize = msgSize;
            Socket = socket;
        }
        public Transfer Receive(int msgSizeOverride = 0) {
            if(msgSizeOverride == 0) {
                msgSizeOverride = MsgSize;
            }

            byte[] bufferIn = new byte[msgSizeOverride];
            Socket.Receive(bufferIn);
            string receivedMessage = Encoding.GetString(bufferIn).TrimEnd('\0');
            if(receivedMessage.Length > 0) {
                string[] attributes = receivedMessage.Split(' ');
                TransferMethod method = (TransferMethod)Enum.Parse(typeof(TransferMethod), attributes[0]);

                string message = string.Empty;
                if (attributes.Length >= 4) {
                    for (int i = 3; i < attributes.Length; i++) {
                        if(i > 3) {
                            message += " ";
                        }
                        message += attributes[i];
                    }
                }
                if (method == TransferMethod.REQUEST) {
                    RequestStatus status = (RequestStatus)Enum.Parse(typeof(RequestStatus), attributes[2]);
                    return new Request(status, message);
                } else if (method == TransferMethod.RESPONSE) {
                    ResponseStatus status = (ResponseStatus)Enum.Parse(typeof(ResponseStatus), attributes[2]);
                    return new Response(status, message);
                } else {
                    throw new ArgumentException("Couldn't receive invalid string.");
                }
            }

            return null;
        }
        public string GetMessage(Transfer transfer) {
            string message = string.Empty;
            if (transfer.Method == TransferMethod.REQUEST) {
                message = $"{transfer.Method} {Name} {(transfer as Request).Status} {transfer.Data}";
            } else { // response
                message = $"{transfer.Method} {Name} {(transfer as Response).Status} {transfer.Data}";
            }

            return message;
        }
        public void Send(Transfer transfer) {
            string message = GetMessage(transfer);
            byte[] bufferOut = Encoding.GetBytes(message);
            Socket.Send(bufferOut);
        }
    }
}
