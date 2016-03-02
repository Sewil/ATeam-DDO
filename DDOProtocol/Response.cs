using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDOProtocol {
    public class Response : Transfer {
        public ResponseStatus Status { get; }
        public Response(ResponseStatus status, string data = null, DataType dataType = DataType.TEXT) : base(data, dataType) {
            Method = TransferMethod.RESPONSE;
            Status = status;
        }
    }
}
