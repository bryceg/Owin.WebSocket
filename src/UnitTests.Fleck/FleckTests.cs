using Fleck;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Fleck
{
    [TestClass]
    public class FleckTests : FleckTestsBase
    {
        [TestMethod]
        public void FeatureParityTest()
        {
            using (var server = new WebSocketServer("http://127.0.0.1:8989"))
            {
                server.Start(ConfigureIntegrationTestConnection);
                SendIntegrationTestMessages();
            }

            AssertIntegrationTestMessages();
        }
    }
}
