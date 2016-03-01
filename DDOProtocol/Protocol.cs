using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDOProtocol
{
    public class Protocol
    {
        public string Name { get; }
        public Encoding Encoding { get; }
        public int MsgSize { get; }
        public Protocol(string name, Encoding encoding, int msgSize)
        {
            Name = name;
            Encoding = encoding;
            MsgSize = msgSize;
        }
        public void Login(string username, string password)
        {
            Send(new Request(this, RequestType.LOGIN, $"{username} {password}"));
            var u = DDOServer.DDOServer.db.Accounts.SingleOrDefault(a => a.Username == username);
            if (u != null)
            {
                if (u.Password == password)
                {
                    Send(new Response(ResponseType.LOGIN_ACCEPTED, $"{username} {password}"));
                }
                else
                {
                    Send(new Response(ResponseType.LOGIN_REJECTED, $"{username} WRONG PASSWORD"));
                }
            }
            else
            {
                Send(new Response(ResponseType.LOGIN_REJECTED, $"{username} DOES NOT EXIST"));
            }
        }
        public void Send(Response response)
        {
            Console.WriteLine($"(REQUEST) {Name} {response.ResponseType} {response.Message}");
        }
        public void Send(Request request)
        {
            Console.WriteLine($"(REQUEST) {Name} {request.RequestType} {request.Message}");
        }
        /*
        Battlefield Application Protocol 1.0



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
        /*
        Definiera upp ett kommunikationsprotokoll med request/response meddelanden och kalla det för DDO/1.0

Data format: Text || Character encoding: UTF8. || Maximum message size: 100 bytes. 

Exempel/Förslag på innehåll: 



(REQUEST) DDO/1.0 LOGIN <username> <password>



(RESPONSE)  DDO/1.0  LOGIN ACCEPTED <username> <password>

(RESPONSE)  DDO/1.0  LOGIN REJECTED <username> DOES NOT EXIST

(RESPONSE)  DDO/1.0  LOGIN REJECTED <username> WRONG PASSWORD

(REQUEST) DDO/1.0 GETSTATE



(RESPONSE)  DDO/1.0  SENDSTATE W 1 1 3 4 5 5 7 5 3 3 M 2 2 4 3 6 5 H 9 9 10 10 P1 0 0 P2 11 4 

(REQUEST) DDO/1.0 MOVE <username> UP



(RESPONSE)  DDO/1.0  MOVE <username> OK



(RESPONSE)  DDO/1.0  MOVE <username> BLOCKED






Och så vidare... Detta är bara några förslag och inte tänkt
        */
    }
}
