using System;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Shared.Editor {
    // The one place that keeps the MCP for Unity bridge out of the test results. The bridge logs a lost websocket as
    // an error (an editor busy in a long fixture drops the connection), and the Test Runner fails a test, a fixture's
    // OneTimeSetUp or the loop tests' error log on any error. While a test run is on, an error whose message carries
    // the bridge's prefix goes to the log as a warning; every other error is passed on unchanged. The run flag lives in
    // SessionState, so it survives the domain reload of a PlayMode run.
    [InitializeOnLoad]
    public sealed class TestLogBridgeErrorFilter : ICallbacks, ILogHandler {
        private const string BRIDGE_MESSAGE_MARK = "MCP-FOR-UNITY";
        private const string RUN_ACTIVE_KEY = "Tests.Shared.Editor.TestLogBridgeErrorFilter.IsRunActive";
        private static readonly TestLogBridgeErrorFilter _filter = new(Debug.unityLogger.logHandler);

        private readonly ILogHandler _innerLogHandler;

        private volatile bool _isRunActive;

        static TestLogBridgeErrorFilter() {
            Debug.unityLogger.logHandler = _filter;
            TestRunnerApi.RegisterTestCallback(_filter);
        }

        private TestLogBridgeErrorFilter(ILogHandler innerLogHandler) {
            _innerLogHandler = innerLogHandler;
            _isRunActive = SessionState.GetBool(RUN_ACTIVE_KEY, false);
        }

        public void RunStarted(ITestAdaptor testsToRun) =>
            SetRunActive(true);

        public void RunFinished(ITestResultAdaptor result) =>
            SetRunActive(false);

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result) { }

        public void LogFormat(LogType logType, Object context, string format, params object[] args) =>
            _innerLogHandler.LogFormat(IsBridgeErrorInRun(logType, format, args) ? LogType.Warning : logType, context, format, args);

        public void LogException(Exception exception, Object context) =>
            _innerLogHandler.LogException(exception, context);

        private void SetRunActive(bool isRunActive) {
            _isRunActive = isRunActive;
            SessionState.SetBool(RUN_ACTIVE_KEY, isRunActive);
        }

        private bool IsBridgeErrorInRun(LogType logType, string format, object[] args) =>
            _isRunActive && logType == LogType.Error && HasBridgeMark(format, args);

        // Debug.LogError(message) passes the message as the only argument of a "{0}" format.
        private static bool HasBridgeMark(string format, object[] args) {
            if (format != null && format.Contains(BRIDGE_MESSAGE_MARK))
                return true;

            foreach (object arg in args ?? Array.Empty<object>()) {
                if (arg is string text && text.Contains(BRIDGE_MESSAGE_MARK))
                    return true;
            }

            return false;
        }
    }
}
