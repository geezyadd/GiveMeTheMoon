#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Tests.Coop.Shared {
    // Two append-only line files in one folder: the client writes its reports, the host writes its commands. Each side
    // only reads complete lines, so a line being written is never half-read.
    public sealed class CoopTestExchange {
        private const string REPORTS_FILE = "client-reports.jsonl";
        private const string COMMANDS_FILE = "host-commands.txt";
        private const char LINE_END = '\n';

        private readonly string _reportsPath;
        private readonly string _commandsPath;

        public CoopTestExchange(string directory) {
            _reportsPath = Path.Combine(directory, REPORTS_FILE);
            _commandsPath = Path.Combine(directory, COMMANDS_FILE);
        }

        public void AppendReport(CoopObservation report) =>
            File.AppendAllText(_reportsPath, JsonUtility.ToJson(report) + LINE_END, Encoding.UTF8);

        public void AppendCommand(string command) =>
            File.AppendAllText(_commandsPath, command + LINE_END, Encoding.UTF8);

        public List<CoopObservation> ReadReports() {
            List<CoopObservation> reports = new List<CoopObservation>();
            foreach (string line in ReadCompleteLines(_reportsPath))
                reports.Add(JsonUtility.FromJson<CoopObservation>(line));

            return reports;
        }

        public bool HasCommand(string command) =>
            ReadCompleteLines(_commandsPath).Contains(command);

        private static List<string> ReadCompleteLines(string path) {
            List<string> lines = new List<string>();
            if (File.Exists(path) == false)
                return lines;

            string text;
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                text = reader.ReadToEnd();

            int lastLineEnd = text.LastIndexOf(LINE_END);
            if (lastLineEnd < 0)
                return lines;

            foreach (string line in text.Substring(0, lastLineEnd).Split(LINE_END)) {
                if (string.IsNullOrWhiteSpace(line) == false)
                    lines.Add(line.Trim());
            }

            return lines;
        }
    }
}
#endif
