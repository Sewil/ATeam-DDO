using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DDOTests
{
    [TestClass]
    class ServerTests
    {
        static DDOServer.Program server = new DDOServer.Program();
        [TestMethod]
        public void ClientsOverflow()
        {            
        }
        [TestCleanup]
        public void CleanUp()
        {
        }
    }
}
