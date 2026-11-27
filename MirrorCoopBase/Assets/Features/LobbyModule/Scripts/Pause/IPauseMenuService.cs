namespace Features.LobbyModule.Scripts.Pause {
    // Opens and closes the pause menu. While it is open the local player's movement and look are off and the cursor
    // is free; the game itself goes on.
    public interface IPauseMenuService {
        public bool IsOpen { get; }

        public void Open();
        public void Close();
    }
}
