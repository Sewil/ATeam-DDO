using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDOProtocol {
    public class Request : Transfer {
        public RequestStatus Status { get; }
        public Request(RequestStatus status, string data = null, DataType dataType = DataType.TEXT) : base(data, dataType) {
            Method = TransferMethod.REQUEST;
            Status = status;
        }
    }
}
