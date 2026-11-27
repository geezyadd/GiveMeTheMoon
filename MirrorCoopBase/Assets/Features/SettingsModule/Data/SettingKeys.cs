namespace Features.SettingsModule.Data {
    // The keys the modules that apply settings read their values by. Each key has a definition in SettingsCatalog
    // (checked by SettingsCatalogGuardTests).
    public static class SettingKeys {
        public const string WINDOW_MODE = "video.window_mode";
        public const string RESOLUTION = "video.resolution";
        public const string VSYNC = "video.vsync";
        public const string FPS_LIMIT = "video.fps_limit";
        public const string SHADOW_QUALITY = "video.shadow_quality";
        public const string ANTI_ALIASING = "video.anti_aliasing";
        public const string RENDER_SCALE = "video.render_scale";
        public const string FIELD_OF_VIEW = "video.field_of_view";
        public const string MOUSE_SENSITIVITY = "controls.mouse_sensitivity";
        public const string INVERT_Y = "controls.invert_y";
        public const string SCREEN_SHAKE = "gameplay.screen_shake";
        public const string NAMEPLATES = "gameplay.nameplates";
        public const string MASTER_VOLUME = "audio.master_volume";
        public const string MUSIC_VOLUME = "audio.music_volume";
        public const string SFX_VOLUME = "audio.sfx_volume";
    }
}
