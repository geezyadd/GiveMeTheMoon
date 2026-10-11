using System;
using System.Globalization;
using System.Text;

namespace Features.PlayerProfileModule.Scripts {
    // Cleans a player name that came from the client before the server shares it with everyone.
    public sealed class PlayerNameSanitizer : IPlayerNameSanitizer {
        private const char SPACE = ' ';
        private const char ELLIPSIS = '\u2026';
        private const int MIN_LENGTH = 2;
        private const string MAX_LENGTH_TOO_SMALL = "The name limit must leave room for one character and the ellipsis.";

        private readonly StringBuilder _builder = new();

        public string Sanitize(string rawName, int maxLength, string fallbackName) {
            if (maxLength < MIN_LENGTH)
                throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, MAX_LENGTH_TOO_SMALL);

            if (string.IsNullOrEmpty(rawName))
                return fallbackName;

            string cleaned = CollapseWhitespace(rawName);
            if (cleaned.Length == 0)
                return fallbackName;

            return cleaned.Length > maxLength ? Shorten(cleaned, maxLength) : cleaned;
        }

        private string CollapseWhitespace(string rawName) {
            _builder.Clear();
            bool pendingSpace = false;
            for (int i = 0; i < rawName.Length; i++) {
                char character = rawName[i];
                if (char.IsWhiteSpace(character)) {
                    pendingSpace = _builder.Length > 0;
                    continue;
                }

                if (IsInvisible(character))
                    continue;

                if (pendingSpace)
                    _builder.Append(SPACE);

                pendingSpace = false;
                _builder.Append(character);
            }

            return _builder.ToString();
        }

        // Control characters break the line layout; format characters (zero-width, right-to-left overrides) hide or
        // flip the text.
        private static bool IsInvisible(char character) =>
            char.IsControl(character) || char.GetUnicodeCategory(character) == UnicodeCategory.Format;

        private static string Shorten(string name, int maxLength) {
            int keep = maxLength - 1;
            if (char.IsHighSurrogate(name[keep - 1]))
                keep--;

            return name.Substring(0, keep).TrimEnd(SPACE) + ELLIPSIS;
        }
    }
}
