#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Globalization;

namespace Tests.Coop.Shared {
    // The coop test client's command line: the host test writes it, the client build reads it.
    public sealed class CoopTestArguments {
        private const string CLIENT_FLAG = "-coopTestClient";
        private const string CONNECT_FLAG = "-connect";
        private const string PORT_FLAG = "-port";
        private const string SCENARIO_FLAG = "-scenario";
        private const string EXCHANGE_FLAG = "-coopDir";
        private const string LOG_FILE_FLAG = "-logFile";
        // A small window: the client must not cover the editor that hosts the test.
        private const string WINDOW_FLAGS = "-screen-fullscreen 0 -screen-width 640 -screen-height 360";

        public string Address { get; }
        public ushort Port { get; }
        public string Scenario { get; }
        public string ExchangeDirectory { get; }

        public CoopTestArguments(string address, ushort port, string scenario, string exchangeDirectory) {
            Address = address;
            Port = port;
            Scenario = scenario;
            ExchangeDirectory = exchangeDirectory;
        }

        public static bool TryRead(string[] commandLine, out CoopTestArguments arguments) {
            arguments = null;
            if (Array.IndexOf(commandLine, CLIENT_FLAG) < 0)
                return false;

            arguments = new CoopTestArguments(
                ReadValue(commandLine, CONNECT_FLAG),
                ushort.Parse(ReadValue(commandLine, PORT_FLAG), CultureInfo.InvariantCulture),
                ReadValue(commandLine, SCENARIO_FLAG),
                ReadValue(commandLine, EXCHANGE_FLAG));
            return true;
        }

        public string ToCommandLine(string logFile) =>
            $"{CLIENT_FLAG} {CONNECT_FLAG} {Address} {PORT_FLAG} {Port.ToString(CultureInfo.InvariantCulture)} "
            + $"{SCENARIO_FLAG} {Scenario} {EXCHANGE_FLAG} \"{ExchangeDirectory}\" {LOG_FILE_FLAG} \"{logFile}\" {WINDOW_FLAGS}";

        private static string ReadValue(string[] commandLine, string flag) {
            int index = Array.IndexOf(commandLine, flag);
            if (index < 0 || index + 1 >= commandLine.Length)
                throw new ArgumentException($"The coop test client needs {flag} <value> on the command line.");

            return commandLine[index + 1];
        }
    }
}
#endif
