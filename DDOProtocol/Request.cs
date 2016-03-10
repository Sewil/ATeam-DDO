using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDOProtocol
{
    public class Request : Message
    {
        public RequestStatus Status { get; }
        public Request(RequestStatus status, DataType dataType = DataType.None, string data = null) : base(dataType, data)
        {
            Method = TransferMethod.Request;
            Status = status;
        }
    }
}
