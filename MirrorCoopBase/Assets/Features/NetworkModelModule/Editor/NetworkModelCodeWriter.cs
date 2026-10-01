using System.Text;

namespace Features.NetworkModelModule.Scripts.Editor {
    public sealed class NetworkModelCodeWriter {
        private readonly StringBuilder _builder = new();
        private int _indent;

        public void Line(string text = "") {
            if (text.Length == 0) {
                _builder.AppendLine();
                return;
            }

            _builder.Append(' ', _indent * 4);
            _builder.AppendLine(text);
        }

        public void Open(string header) {
            Line(header + " {");
            _indent++;
        }

        public void Close() {
            _indent--;
            Line("}");
        }

        public override string ToString() =>
            _builder.ToString();
    }
}
