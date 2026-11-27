namespace Features.SettingsModule.UI {
    // Opens the settings window and asks it to close (it may first ask about unapplied changes).
    public interface ISettingsWindowService {
        public bool IsOpen { get; }

        public void Open();

        // Closes the window, or first shows the "unapplied changes" question or closes an open question.
        public void RequestClose();
    }
}
