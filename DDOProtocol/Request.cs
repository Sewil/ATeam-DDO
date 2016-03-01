using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DDOServer;

namespace DDOProtocol
{
    public class Request
    {
        public Protocol Protocol { get; }
        public RequestType RequestType { get; }
        public string Message { get; }
        public Request(Protocol protocol, RequestType requestType, string message)
        {
            Protocol = protocol;
            RequestType = requestType;
            Message = message;
        }
        
        /*
        Data format: Text.  //  Character encoding: UTF8. //  Maximum message 
size: 100 bytes. 



Request message: "BAP/1.0 SHOT Row Column",  where "Row" is the row 
number and "Column" is the column number,  for example "BAP/1.0 
SHOT 1 2". 



Response message: "BAP/1.0 HIT Row Column IsHit",  where "IsHit" is 
either "TRUE" or "FALSE", and "Row" is the row number and "Column" is 
the column number,  for example "BAP/1.0 HIT 1 2 FALSE".



Response error message: "BAP/1.0 ERROR Message", where "Message" is 
a text string, for example "BAP/1.0 ERROR Incorrect request.".
        */
    }
}
