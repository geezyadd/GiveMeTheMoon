namespace Features.PlayerProfileModule.Scripts {
    public interface IPlayerNameSanitizer {
        public string Sanitize(string rawName, int maxLength, string fallbackName);
    }
}
