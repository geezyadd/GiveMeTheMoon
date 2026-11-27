namespace Features.SettingsModule.Data {
    // The id of a resolution option: "<width>x<height>".
    public static class ResolutionOptionId {
        private const char SEPARATOR = 'x';

        public static string Format(int width, int height) =>
            $"{width}{SEPARATOR}{height}";

        public static bool TryParse(string optionId, out int width, out int height) {
            width = 0;
            height = 0;
            if (string.IsNullOrEmpty(optionId))
                return false;

            string[] parts = optionId.Split(SEPARATOR);
            return parts.Length == 2
                   && int.TryParse(parts[0], out width) && width > 0
                   && int.TryParse(parts[1], out height) && height > 0;
        }
    }
}
