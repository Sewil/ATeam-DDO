using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDOProtocol {
    public abstract class Transfer {
        public string Data { get; }
        public DataType DataType { get; }
        public TransferMethod Method { get; protected set; }
        public Transfer(string data = null, DataType dataType = DataType.TEXT) {
            Data = data;
            DataType = dataType;
        }
    }
}
