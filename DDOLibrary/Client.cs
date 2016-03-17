
using DDOLibrary.GameObjects;
using DDOLibrary.Protocol;

namespace DDOLibrary {
    public class Client {
        public Response LatestResponse { get; set; }
        public bool IsHeard { get; set; }
        public Protocol.Protocol Protocol { get; }
        public Player Player { get; set; }
        public bool IsLoggedIn { get { return Account != null; } }
        public Account Account { get; set; }
        public string IPAddress {
            get {
                if (Protocol != null && Protocol.Socket != null) {
                    return Protocol.Socket.RemoteEndPoint.ToString();
                } else {
                    return null;
                }
            }
        }
        public Client(Protocol.Protocol protocol) {
            Protocol = protocol;
        }
    }
}
