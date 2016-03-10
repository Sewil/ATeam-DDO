using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDOProtocol
{
    public class Response : Message
    {
        public ResponseStatus Status { get; }
        public Response(ResponseStatus status, DataType dataType = DataType.None, string data = null) : base(dataType, data)
        {
            Method = TransferMethod.Response;
            Status = status;
        }
    }
}
