#if UNITY_INCLUDE_TESTS
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tests.Coop.Shared;

namespace Tests.Coop.Host {
    // The client build as a child process of the test: started with the coop arguments, always killed on Dispose.
    public sealed class CoopClientProcess : IDisposable {
        private const int KILL_WAIT_MILLISECONDS = 5000;
        private const int LOG_TAIL_LINES = 40;

        private readonly Process _process;
        private readonly string _logFile;

        public bool HasExited =>
            _process.HasExited;

        private CoopClientProcess(Process process, string logFile) {
            _process = process;
            _logFile = logFile;
        }

        public static CoopClientProcess Start(CoopTestArguments arguments, string logFile) {
            string executable = CoopClientBuildPaths.Executable;
            Assert.IsTrue(File.Exists(executable), $"No coop test client at {executable}: build it with Tools > Coop Test > Build Client.");
            ProcessStartInfo startInfo = new ProcessStartInfo(executable, arguments.ToCommandLine(logFile)) {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(executable)
            };
            return new CoopClientProcess(Process.Start(startInfo), logFile);
        }

        public void Dispose() {
            if (_process.HasExited == false) {
                _process.Kill();
                _process.WaitForExit(KILL_WAIT_MILLISECONDS);
            }

            _process.Dispose();
        }

        public string DescribeLogTail() {
            if (File.Exists(_logFile) == false)
                return $"(no client log at {_logFile})";

            string text;
            using (FileStream stream = new FileStream(_logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (StreamReader reader = new StreamReader(stream))
                text = reader.ReadToEnd();

            string[] lines = text.Split('\n');
            return $"Client log {_logFile}, last {LOG_TAIL_LINES} lines:\n" + string.Join("\n", lines.Skip(Math.Max(0, lines.Length - LOG_TAIL_LINES)));
        }
    }
}
#endif
