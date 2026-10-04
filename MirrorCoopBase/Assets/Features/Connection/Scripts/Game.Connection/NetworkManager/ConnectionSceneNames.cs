namespace Game.Connection
{
    // Scenes a session moves between; the names are set on ConnectionNetworkManager in GlobalScene.
    internal sealed class ConnectionSceneNames
    {
        public ConnectionSceneNames(string lobby, string menu, string game, string persistent)
        {
            Lobby = lobby;
            Menu = menu;
            Game = game;
            Persistent = persistent;
        }

        public string Lobby { get; }
        public string Menu { get; }
        public string Game { get; }
        public string Persistent { get; }
    }
}
