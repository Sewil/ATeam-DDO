using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DDOTests {
    [TestClass]
    class ServerTests {
        static DDOServer.DDOServer server = new DDOServer.DDOServer();

        [TestMethod]
        public void ClientsOverflow() {
            
        }

        [TestCleanup]
        public void CleanUp() {
        }
    }
}
