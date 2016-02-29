using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDOProtocol {
    public class Response {
        public string Message { get; }
        public ResponseType ResponseType { get; }
        public Response(ResponseType responseType, string message) {
            ResponseType = responseType;
        }
    }
}
