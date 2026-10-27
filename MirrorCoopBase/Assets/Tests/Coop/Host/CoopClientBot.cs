#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using Game.Connection;
using Mirror;
using NUnit.Framework;
using Tests.Coop.Shared;

namespace Tests.Coop.Host {
    // One client process of the connection tests as the host sees it: its own exchange folder, the commands sent to it,
    // its reports, its connection and player on the server (found by the name it authenticated with).
    public sealed class CoopClientBot : IDisposable {
        private const string CLIENT_LOG_FILE = "client.log";

        private readonly CoopTestExchange _exchange;
        private CoopClientProcess _process;
        private int _readReports;

        public string Name { get; }
        public string ExchangeDirectory { get; }
        public CoopObservation LastReport { get; private set; }
        public bool HasExited =>
            _process == null || _process.HasExited;

        private CoopClientBot(string name, string directory, CoopTestExchange exchange, CoopClientProcess process) {
            Name = name;
            ExchangeDirectory = directory;
            _exchange = exchange;
            _process = process;
        }

        // Every process gets a folder of its own, so a relaunched client starts with no commands of its predecessor.
        public static CoopClientBot Start(string runDirectory, string folder, string name, string address, ushort port) {
            string directory = Path.Combine(runDirectory, folder);
            Directory.CreateDirectory(directory);
            CoopTestArguments arguments = new CoopTestArguments(address, port, CoopConnectionCommands.SCENARIO, directory);
            CoopClientProcess process = CoopClientProcess.Start(arguments, Path.Combine(directory, CLIENT_LOG_FILE));
            return new CoopClientBot(name, directory, new CoopTestExchange(directory), process);
        }

        public void Dispose() =>
            Kill();

        // Kills the process at once, as a crash would: no Leave, no goodbye to the server.
        public void Kill() {
            _process?.Dispose();
            _process = null;
        }

        public void Send(string command, string argument) {
            LastReport = null;
            _exchange.AppendCommand(string.IsNullOrEmpty(argument) ? command : CoopConnectionCommands.With(command, argument));
        }

        // Takes the next report: false while none came; fails the test when the client failed or answered another step.
        public bool TryTakeReport(string step) {
            List<CoopObservation> reports = _exchange.ReadReportsFrom(_readReports);
            if (reports.Count == 0)
                return false;

            CoopObservation report = reports[0];
            _readReports++;
            if (report.Step == CoopTestSteps.FAILED)
                Assert.Fail($"{Name} failed: {report.Message}\n{string.Join("\n", report.Errors)}\n{DescribeLogTail()}");

            Assert.AreEqual(step, report.Step, $"{Name} reported another step than expected: {report.Describe()}");
            LastReport = report;
            return true;
        }

        public string DescribeLogTail() =>
            _process == null ? $"({Name}: no running process)" : _process.DescribeLogTail();

        public bool TryGetConnection(out NetworkConnectionToClient connection) {
            foreach (NetworkConnectionToClient candidate in NetworkServer.connections.Values) {
                if (candidate == NetworkServer.localConnection || candidate.isAuthenticated == false)
                    continue;

                if (ConnectionAuthenticator.GetAuthRequest(candidate).playerName == Name) {
                    connection = candidate;
                    return true;
                }
            }

            connection = null;
            return false;
        }

        public bool TryGetPlayer(out NetworkIdentity player) {
            player = TryGetConnection(out NetworkConnectionToClient connection) ? connection.identity : null;
            return player != null;
        }

        public bool IsGone() =>
            TryGetConnection(out NetworkConnectionToClient _) == false;
    }
}
#endif
