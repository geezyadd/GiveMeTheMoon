using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Shared.Editor {
    // The bridge's error goes to the log of a running test as a warning; any other error stays an error.
    public sealed class TestLogBridgeErrorFilterTests {
        private const string BRIDGE_ERROR = "<b><color=#cc3333>MCP-FOR-UNITY</color></b>: [WebSocket] Registration with server failed: test message.";
        private const string OTHER_ERROR = "Another error: test message.";

        [Test]
        public void WhenTheBridgeLogsAnErrorInATest_ThenTheTestPasses() =>
            Debug.LogError(BRIDGE_ERROR);

        [Test]
        public void WhenAnotherErrorIsLoggedInATest_ThenItStaysAnError() {
            LogAssert.Expect(LogType.Error, OTHER_ERROR);
            Debug.LogError(OTHER_ERROR);
        }
    }
}
